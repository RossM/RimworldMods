using XylXenos.Patches;

namespace XylXenos;

public interface INotificationListener
{
    public void RegisterWith(NotificationManager manager);
}

public enum NotificationEvent
{
    /// <summary>
    /// Called before <see cref="Thing.TakeDamage"/>.
    ///
    /// This hook passes a <see cref="DamageInfo"/> as its "data" parameter.
    /// </summary>
    PreDamageTaken,

    /// <summary>
    /// Called after <see cref="Pawn_GeneTracker.Notify_GenesChanged"/>.
    /// </summary>
    PostGenesChanged,

    // PostHediffsChanged and PostHediffStateChange are generally both called when a pawn's hediffs change, but
    // PostHediffsChanged is called as soon as the HediffSet is updated, while PostHediffStateChange is called
    // slightly later when the game is checking for the results of the changes. I'm not sure if having both is
    // really necessary but out of caution I'm leaving them both here for now.

    /// <summary>
    /// Called after <see cref="HediffSet.DirtyCache"/>.
    /// </summary>
    PostHediffsChanged,

    /// <summary>
    /// Called after <see cref="Pawn_HealthTracker.CheckForStateChange"/>.
    /// </summary>
    PostHediffStateChange,

    /// <summary>
    /// Called after <see cref="Pawn_ApparelTracker.Notify_ApparelChanged"/>.
    /// </summary>
    PostApparelChanged,

    /// <summary>
    /// Called after <see cref="GeneUtility.SatisfyChemicalGenes"/>.
    /// </summary>
    PostSatisfyGenes,

    /// <summary>
    /// Called after <see cref="Pawn.Discard"/>.
    /// </summary>
    PostDiscard,

    /// <summary>
    /// Called after <see cref="Pawn.PostMake"/>.
    /// </summary>
    PostPostMake,

    /// <summary>
    /// Called after <see cref="NotificationManager.LoadedGame"/>, immediately after the notification manager
    /// has called <see cref="INotificationListener.RegisterWith"/> on all listeners.
    /// </summary>
    PostLoadedGame,

    /// <summary>
    /// Called after <see cref="Game.Dispose"/>, immediately before the notification manager unregisters all listeners.
    ///
    /// This hook passes null as its "pawn" parameter, so can only be used as a global hook.
    /// </summary>
    GlobalPostGameDispose,

    /// <summary>
    /// Called inside <see cref="PawnGenerator.TryGenerateNewPawnInternal"/> before the <see cref="Pawn"/>'s bio and name are generated.
    /// This hook can be used to modify the pawn's <see cref="Gender"/> and <see cref="XenotypeDef"/> during generation.
    ///
    /// This hook passes a <see cref="Patch_PawnGenerator.PawnGenerationEarlyData"/> as the "data" parameter.
    /// </summary>
    PawnGenerationEarly,
}

/// <summary>
///     This enables listeners to register for global or pawn-specific callbacks which are triggered by patched
///     hooks. This makes it easy to write genes, hediffs, ThingComps, and so on that react to events without
///     needing specific patches to wire the correct events to each listener. Implement
///     <see cref="INotificationListener" />
///     and register for the needed callbacks in <see cref="INotificationListener.RegisterWith" />.
/// </summary>
[UsedFromReflection]
public class NotificationManager : GameComponent
{
    private class EventInfo
    {
        public readonly List<CallbackInfo> globalCallbacks = [];
        public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
    }

    private class RegistrationInfo(NotificationEvent eventType, Thing target)
    {
        public readonly NotificationEvent eventType = eventType;
        public readonly bool isGlobal = target == null;
        public readonly System.WeakReference<Thing> target = target == null ? null : new(target);
    }

    public struct CallbackInfo
    {
        public Delegate wrappedCallback;
        public INotificationListener listener;
        public string name;
    }

    public static NotificationManager Instance => Current.Game.GetComponent<NotificationManager>();
    private static bool doDebug = false;

    public static List<INotificationListener> staticListeners = [];
    public HashSet<INotificationListener> alreadyRegisteredStaticListeners = new();

    private readonly EventInfo[] events = new EventInfo[Enum.GetValues(typeof(NotificationEvent)).Length];

    private ConditionalWeakTable<INotificationListener, List<RegistrationInfo>> registeredEvents = new();

    public NotificationManager(Game _)
    {
    }

    [DebugAction(allowedGameStates = 0)]
    public static void ToggleNotificationManagerLogging()
    {
        doDebug = !doDebug;
    }

    private void RegisterInternal<T>(
        NotificationEvent eventType,
        Thing target,
        Action<Thing, T> callback,
        object source,
        string name)
    {
        if (source is not INotificationListener listener)
            throw new InvalidOperationException("Only an INotificationListener can register for notifications");

        if (doDebug)
            Debug.Log(
                $"Register eventType={eventType} {(target == null ? "global" : $"target={target}")} listener={listener} name={name}");

        var records = registeredEvents.GetOrCreateValue(listener);
        records.Add(new(eventType, target));

        EventInfo eventInfo = events[(int)eventType] ??= new();

        CallbackInfo callbackInfo = new() { wrappedCallback = callback, listener = listener, name = name };

        if (target == null)
        {
            if (eventInfo.globalCallbacks.Any(c => c.listener == listener && c.name == name))
            {
                Log.Warning(
                    $"Adding a duplicate callback: type={eventType} global listener={listener} name={name}");
                return;
            }

            eventInfo.globalCallbacks.Add(callbackInfo);
        }
        else
        {
            List<CallbackInfo> localCallbacks = eventInfo.localCallbacks.GetOrCreateValue(target);
            if (localCallbacks.Any(c => c.listener == listener && c.name == name))
            {
                Log.Warning(
                    $"Adding a duplicate callback: type={eventType} target={target} listener={listener} name={name}");
                return;
            }

            localCallbacks.Add(callbackInfo);
        }
    }

    public void Register<T>(NotificationEvent eventType, Thing target, Action<Thing, T> callback)
    {
        RegisterInternal(eventType, target, callback, callback.Target, callback.Method.Name);
    }

    public void Register<T>(NotificationEvent eventType, Thing target, Action<T> callback)
    {
        RegisterInternal<T>(eventType, target, (_, data) => callback(data), callback.Target, callback.Method.Name);
    }

    // ReSharper disable once UnusedMember.Global
    public void Register(NotificationEvent eventType, Thing target, Action<Thing> callback)
    {
        RegisterInternal<object>(eventType, target, (t, _) => callback(t), callback.Target, callback.Method.Name);
    }

    public void Register(NotificationEvent eventType, Thing target, Action callback)
    {
        RegisterInternal<object>(eventType, target, (_, _) => callback(), callback.Target, callback.Method.Name);
    }

    public void UnregisterAll(INotificationListener listener)
    {
        if (!registeredEvents.TryGetValue(listener, out List<RegistrationInfo> records))
            return;

        foreach (var record in records)
        {
            if (record.isGlobal)
                events[(int)record.eventType]?.globalCallbacks.RemoveAll(callback => callback.listener == listener);
            else
            {
                if (!record.target.TryGetTarget(out Thing target))
                    continue;

                if (events[(int)record.eventType]?.localCallbacks?.TryGetValue(target, out var callbacks) == true)
                    callbacks.RemoveAll(callback => callback.listener == listener);
            }
        }

        registeredEvents.Remove(listener);
    }

    public void Notify(NotificationEvent eventType, Thing target, object data = null)
    {
        if (Scribe.mode != LoadSaveMode.Inactive)
            return;

        if (doDebug)
            Debug.Log($"Notify eventType={eventType} target={target} data={data}");

        EventInfo eventInfo = events[(int)eventType];
        if (eventInfo == null)
            return;

        foreach (CallbackInfo callbackInfo in eventInfo.globalCallbacks)
        {
            DoNotify(callbackInfo, target, data, eventType);
        }

        if (target == null)
            return;

        if (eventInfo.localCallbacks.TryGetValue(target, out List<CallbackInfo> callbackInfos))
        {
            foreach (CallbackInfo callbackInfo in callbackInfos)
            {
                DoNotify(callbackInfo, target, data, eventType);
            }
        }
    }

    private static void DoNotify(CallbackInfo callbackInfo, Thing target, object data, NotificationEvent eventType)
    {
        switch (callbackInfo.listener)
        {
            // ReSharper disable SuspiciousTypeConversion.Global
            case Thing { Destroyed: true }:
            case ThingComp t when t.parent.Destroyed:
            // ReSharper restore SuspiciousTypeConversion.Global
            case MapComponent m when m.map.Disposed:
            case GeneExt { Removed: true }:
                Log.Warning($"A destroyed thing got an event: {callbackInfo.listener} : {callbackInfo.name} ({eventType} on {target})");
                return;
        }

        if (doDebug)
            Debug.Log($"  {callbackInfo.listener} : {callbackInfo.name}");

        try
        {
            callbackInfo.wrappedCallback.DynamicInvoke(target, data);
        }
        catch (Exception exception)
        {
            if (Prefs.DevMode)
            {
                Log.Error($"Exception notifying {callbackInfo.listener} : {callbackInfo.name} ({eventType} on {target}): {exception}");
            }
            else if (callbackInfo.listener != null)
            {
                Log.ErrorOnce(
                    $"Exception notifying {callbackInfo.listener} : {callbackInfo.name} ({eventType} on {target}). Suppressing further errors. Exception: {exception}",
                    callbackInfo.listener.GetHashCode() ^ 0x1c502196);
            }
        }
    }

    private void CallRegistrationHandlers(object thing)
    {
        if (thing is INotificationListener target)
            target.RegisterWith(this);

        switch (thing)
        {
            case HediffWithComps hediffWithComps:
            {
                foreach (HediffComp comp in hediffWithComps.comps ?? Enumerable.Empty<HediffComp>())
                    CallRegistrationHandlers(comp);
                break;
            }
            case Pawn pawn:
            {
                foreach (Gene gene in pawn.genes?.GenesListForReading ?? Enumerable.Empty<Gene>())
                    CallRegistrationHandlers(gene);
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs ?? Enumerable.Empty<Hediff>())
                    CallRegistrationHandlers(hediff);
                break;
            }
        }
    }

    public override void LoadedGame()
    {
        using var _ = new ProfileBlock();

        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            CallRegistrationHandlers(pawn);
        foreach (var listener in staticListeners)
            listener.RegisterWith(this);

        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            Notify(NotificationEvent.PostLoadedGame, pawn);
    }

    public override void FinalizeInit()
    {
        foreach (var listener in staticListeners)
        {
            if (alreadyRegisteredStaticListeners.Contains(listener))
                continue;

            listener.RegisterWith(this);
            alreadyRegisteredStaticListeners.Add(listener);
        }
    }

    public void Reset()
    {
        registeredEvents = new();
        for (int i = 0; i < events.Length; i++)
            events[i] = null;
        alreadyRegisteredStaticListeners.Clear();
    }
}
