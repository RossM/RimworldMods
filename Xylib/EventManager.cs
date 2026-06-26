namespace Xylib;

public class PawnGenerationData(PawnGenerationRequest request, XenotypeDef xenotype)
{
    public PawnGenerationRequest request = request;
    public XenotypeDef xenotype = xenotype;
}

public interface IEventListener
{
    /// <summary>
    ///     Called when a listener is created or loaded. The listener should call
    ///     <see cref="O:EventManager.Register" />
    ///     to register for any notifications they want to receive.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="EventManager" /> that should be registered with. This is always
    ///     <see cref="EventManager.Instance" />.
    /// </param>
    public void RegisterWith(EventManager manager);

    /// <summary>
    ///     Called when a listener is about to be removed from the notification manager. Notifications registered by the
    ///     listener directly
    ///     will be removed automatically, but if there are any child objects such as Comps that need to be unregistered, call
    ///     <see cref="EventManager.RemoveListener" /> for each one.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="EventManager" /> that should be unregistered with. This is always
    ///     <see cref="EventManager.Instance" />.
    /// </param>
    public void PreUnregister(EventManager manager);
}

public class EventDef : Def
{
    public bool global = false;
    public Type dataType = null;
}

[DefOf]
public static class EventDefOf
{
    /// <summary>
    ///     Called after <see cref="Game.Dispose" />, immediately before the notification manager unregisters all listeners.
    ///     This hook passes null as its "pawn" parameter, so can only be used as a global hook.
    /// </summary>
    public static EventDef GlobalPostGameDispose;

    /// <summary>
    ///     Called inside <see cref="StartingPawnUtility.GeneratePossessions" />.
    ///     This hook passes a <see cref="List&lt;ThingDefCount&gt;" /> as the "data" parameter.
    /// </summary>
    public static EventDef InGeneratePossessions;

    /// <summary>
    ///     Called after <see cref="Pawn_ApparelTracker.Notify_ApparelChanged" />.
    /// </summary>
    public static EventDef PostApparelChanged;

    /// <summary>
    ///     Called after <see cref="Pawn_AgeTracker.BirthdayBiological" />.
    /// </summary>
    public static EventDef PostBirthday;

    /// <summary>
    ///     Called after <see cref="Pawn_HealthTracker.CheckForStateChange" />.
    /// </summary>
    public static EventDef PostCheckForStateChange;

    /// <summary>
    ///     Called after <see cref="Thing.Discard" />.
    /// </summary>
    public static EventDef PostDiscard;

    /// <summary>
    ///     Called after <see cref="Pawn_HealthTracker.MakeDowned" />.
    /// </summary>
    public static EventDef PostDowned;

    /// <summary>
    ///     Called after <see cref="PawnGenerator.GenerateInitialHediffs" />.
    /// </summary>
    public static EventDef PostGenerateInitialHediffs;

    /// <summary>
    ///     Called after <see cref="PawnGenerator.GenerateNewPawnInternal" />.
    ///     This hook passes a <see cref="PawnGenerationRequest" /> as its "data" parameter.
    /// </summary>
    public static EventDef PostGenerateNewPawn;

    /// <summary>
    ///     Called after <see cref="Pawn_GeneTracker.Notify_GenesChanged" />.
    /// </summary>
    public static EventDef PostGenesChanged;

    // PostHediffsChanged and PostHediffStateChange are generally both called when a pawn's hediffs change, but
    // PostHediffsChanged is called as soon as the HediffSet is updated, while PostHediffStateChange is called
    // slightly later when the game is checking for the results of the changes. I'm not sure if having both is
    // really necessary but out of caution I'm leaving them both here for now.

    /// <summary>
    ///     Called after <see cref="HediffSet.DirtyCache" />.
    /// </summary>
    public static EventDef PostHediffsChanged;

    /// <summary>
    ///     Called after <see cref="EventManager.LoadedGame" />, immediately after the notification manager
    ///     has called <see cref="IEventListener.RegisterWith" /> on all listeners.
    /// </summary>
    public static EventDef PostLoadedGame;

    /// <summary>
    ///     Called after a pawn does lovin'.
    ///     This hook passes a <see cref="Pawn" /> as its "data" parameter.
    /// </summary>
    public static EventDef PostLovin;


    /// <summary>
    ///     Called after <see cref="Pawn.Kill(Verse.DamageInfo?,Hediff)" />.
    /// </summary>
    public static EventDef PostPawnKilled;

    /// <summary>
    ///     Called after <see cref="Thing.PostMake" />.
    /// </summary>
    public static EventDef PostPostMake;

    /// <summary>
    ///     Called after <see cref="PawnGenerator.RedressPawn" />.
    ///     This hook passes a <see cref="PawnGenerationRequest" /> as its "data" parameter.
    /// </summary>
    public static EventDef PostRedressPawn;

    /// <summary>
    ///     Called after <see cref="GeneUtility.SatisfyChemicalGenes" />.
    /// </summary>
    public static EventDef PostSatisfyChemicalGenes;

    /// <summary>
    ///     Called inside <see cref="PawnGenerator.TryGenerateNewPawnInternal" /> before the <see cref="Pawn" />'s bio and name
    ///     are generated.
    ///     This hook can be used to modify the pawn's <see cref="Gender" /> and <see cref="XenotypeDef" /> during generation.
    ///     This hook passes a <see cref="PawnGenerationData" /> as the "data" parameter.
    /// </summary>
    public static EventDef PreGeneratePawnBioAndName;

    /// <summary>
    ///     Called before <see cref="Thing.TakeDamage" />.
    ///     This hook passes a <see cref="DamageInfo" /> as its "data" parameter.
    /// </summary>
    public static EventDef PreTakeDamage;

    static EventDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(EventDefOf));
    }
}

/// <summary>
///     This enables listeners to register for global or pawn-specific callbacks which are triggered by patched
///     hooks. This makes it easy to write genes, hediffs, ThingComps, and so on that react to events without
///     needing specific patches to wire the correct events to each listener. Implement
///     <see cref="IEventListener" />
///     and register for the needed callbacks in <see cref="IEventListener.RegisterWith" />.
/// </summary>
[UsedFromReflection]
public class EventManager : GameComponent
{
    private class NotificationInfo
    {
        public bool usesPriority = false;
        public readonly List<CallbackInfo> globalCallbacks = [];
        public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
    }

    private class RegistrationInfo(EventDef eventDef, Thing target)
    {
        public readonly EventDef eventDef = eventDef;
        public readonly bool isGlobal = target == null;
        public readonly System.WeakReference<Thing> target = target == null ? null : new(target);
    }

    public struct CallbackInfo
    {
        public Action<Thing, object> wrappedCallback;
        public IEventListener listener;
        public string name;
        public int priority;

        public override string ToString()
        {
            return $"{name}[{listener}]";
        }
    }

    public static EventManager Instance => Current.Game?.GetComponent<EventManager>();
    private static bool doDebug = false;

    private static readonly List<IEventListener> staticListeners = [];
    [Unsaved] private HashSet<IEventListener> alreadyRegisteredStaticListeners = [];

    private readonly NotificationInfo[] notifications = new NotificationInfo[DefDatabase<EventDef>.DefCount];

    private ConditionalWeakTable<IEventListener, List<RegistrationInfo>> registrations = new();

    private readonly List<CallbackInfo> tempCallbacks = [];

    public EventManager(Game _)
    {
    }

    [DebugAction(allowedGameStates = 0)]
    public static void ToggleNotificationManagerLogging()
    {
        doDebug = !doDebug;
    }

    private void RegisterInternal(
        EventDef eventDef,
        Thing target,
        Action<Thing, object> callback,
        object source,
        string name,
        int priority)
    {
        if (source is not IEventListener listener)
            throw new InvalidOperationException("Only an INotificationListener can register for notifications");

        if (doDebug)
            Debug.Log(
                $"[EventManager] Register eventDef={eventDef} {(target == null ? "global" : $"target=[{target}]")} listener={listener} name={name} priority={priority}");

        var records = registrations.GetOrCreateValue(listener);
        records.Add(new(eventDef, target));

        NotificationInfo notificationInfo = notifications[eventDef.index] ??= new();
        if (priority != 0)
            notificationInfo.usesPriority = true;

        CallbackInfo callbackInfo = new() { wrappedCallback = callback, listener = listener, name = name, priority = priority };

        if (target == null)
        {
            if (notificationInfo.globalCallbacks.Any(c => c.listener == listener && c.name == name))
            {
                Log.Warning(
                    $"[EventManager] Adding a duplicate callback: type={eventDef} global listener={listener} name={name}");
                return;
            }

            notificationInfo.globalCallbacks.Add(callbackInfo);
        }
        else
        {
            List<CallbackInfo> localCallbacks = notificationInfo.localCallbacks.GetOrCreateValue(target);
            if (localCallbacks.Any(c => c.listener == listener && c.name == name))
            {
                Log.Warning(
                    $"[EventManager] Adding a duplicate callback: type={eventDef} target={target} listener={listener} name={name}");
                return;
            }

            localCallbacks.Add(callbackInfo);
        }
    }

    public void Register<T>(EventDef eventDef, Thing target, [NotNull] Action<Thing, T> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (eventDef == null)
        {
            Log.WarningOnce("[EventManager] eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        if (!typeof(T).IsAssignableFrom(eventDef.dataType))
        {
            Log.ErrorOnce(
                $"[EventManager] Registered callback for {callback.Target.GetType()} {eventDef.defName} expects {typeof(T)} but event will pass {eventDef.dataType}",
                Gen.HashCombineInt(0x467A56FF, eventDef.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(eventDef, target, (t, data) => callback(t, (T)data), callback.Target, MethodName(callback), priority);
    }

    public void Register<T>(EventDef eventDef, Thing target, [NotNull] Action<T> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (eventDef == null)
        {
            Log.WarningOnce("[EventManager] eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        if (!typeof(T).IsAssignableFrom(eventDef.dataType))
        {
            Log.ErrorOnce(
                $"[EventManager] Registered callback for {callback.Target.GetType()} {eventDef.defName} expects {typeof(T)} but event will pass {eventDef.dataType}",
                Gen.HashCombineInt(0x467A56FF, eventDef.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(eventDef, target, (_, data) => callback((T)data), callback.Target, MethodName(callback), priority);
    }

    // ReSharper disable once UnusedMember.Global
    public void Register(EventDef eventDef, Thing target, [NotNull] Action<Thing> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (eventDef == null)
        {
            Log.WarningOnce("[EventManager] eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        RegisterInternal(eventDef, target, (t, _) => callback(t), callback.Target, MethodName(callback), priority);
    }

    public void Register(EventDef eventDef, Thing target, [NotNull] Action callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (eventDef == null)
        {
            Log.WarningOnce("[EventManager] eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        RegisterInternal(eventDef, target, (_, _) => callback(), callback.Target, MethodName(callback), priority);
    }

    private static string MethodName(Delegate fn)
    {
        return $"{fn.Method.DeclaringType?.FullName ?? "<global>"}.{fn.Method.Name}";
    }

    // ReSharper disable once UnusedMember.Global
    public void Register<T>(
        [NotNull] IEventListener listener,
        EventDef eventDef,
        Thing target,
        [NotNull] Action<Thing, T> callback)
    {
        if (listener == null)
            throw new ArgumentNullException(nameof(listener));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (eventDef == null)
        {
            Log.WarningOnce("[EventManager] eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, listener.GetHashCode()));
            return;
        }

        if (!typeof(T).IsAssignableFrom(eventDef.dataType))
        {
            Log.ErrorOnce(
                $"[EventManager] Registered callback for {callback.Target.GetType()} {eventDef.defName} expects {typeof(T)} but event will pass {eventDef.dataType}",
                Gen.HashCombineInt(0x467A56FF, eventDef.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(eventDef, target, (t, data) => callback(t, (T)data), listener,
            $"{listener.GetType().FullName}.<{eventDef.defName}>", 0);
    }

    public void RemoveListener(IEventListener listener)
    {
        listener.PreUnregister(this);

        if (!registrations.TryGetValue(listener, out List<RegistrationInfo> records))
            return;

        foreach (var record in records)
        {
            if (record.isGlobal)
                notifications[record.eventDef.index]?.globalCallbacks.RemoveAll(callback => callback.listener == listener);
            else
            {
                if (!record.target.TryGetTarget(out Thing target))
                    continue;

                if (notifications[record.eventDef.index]?.localCallbacks?.TryGetValue(target, out List<CallbackInfo> callbacks) == true)
                    callbacks.RemoveAll(callback => callback.listener == listener);
            }
        }

        registrations.Remove(listener);
    }

    public void Notify(EventDef eventDef, Thing target, object data = null)
    {
        if (Scribe.mode != LoadSaveMode.Inactive)
            return;

        if (doDebug)
            Debug.Log($"[EventManager] Notify eventDef={eventDef} target=[{target}] data={data}");

        if (Prefs.DevMode)
        {
            if (eventDef.global && target != null)
            {
                Log.ErrorOnce($"[EventManager] Notification {eventDef.defName} is global but was called with target {target}",
                    Gen.HashCombineInt(0x34330AEF, eventDef.index));
            }

            if (!eventDef.global && target == null)
            {
                Log.ErrorOnce($"[EventManager] Notification {eventDef.defName} is not global but was called with null target",
                    Gen.HashCombineInt(0x140A0CA2, eventDef.index));
            }

            if (eventDef.dataType != null && data == null)
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} should take data of type {eventDef.dataType} but was given null",
                    Gen.HashCombineInt(0xEEB8AC2, eventDef.index));
            }

            if (eventDef.dataType != null && data != null && !eventDef.dataType.IsAssignableFrom(data.GetType()))
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} should take data of type {eventDef.dataType} but was given {data.GetType()}",
                    Gen.HashCombineInt(0x4D53041B, eventDef.index));
            }

            if (eventDef.dataType == null && data != null)
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} shouldn't take data but was given {data.GetType()}",
                    Gen.HashCombineInt(0x7A213146, eventDef.index));
            }
        }

        NotificationInfo notificationInfo = notifications[eventDef.index];
        if (notificationInfo == null)
            return;

        List<CallbackInfo> localCallbacks;

        if (!notificationInfo.usesPriority)
        {
            foreach (CallbackInfo callbackInfo in notificationInfo.globalCallbacks)
            {
                DoNotify(eventDef, callbackInfo, target, data);
            }

            if (target == null || !notificationInfo.localCallbacks.TryGetValue(target, out localCallbacks))
                return;

            foreach (CallbackInfo callbackInfo in localCallbacks)
            {
                DoNotify(eventDef, callbackInfo, target, data);
            }

            return;
        }

        tempCallbacks.Clear();
        tempCallbacks.AddRange(notificationInfo.globalCallbacks);
        if (target != null && notificationInfo.localCallbacks.TryGetValue(target, out localCallbacks))
            tempCallbacks.AddRange(localCallbacks);
        tempCallbacks.SortByDescending(callback => callback.priority);

        foreach (CallbackInfo callbackInfo in tempCallbacks)
        {
            DoNotify(eventDef, callbackInfo, target, data);
        }

        tempCallbacks.Clear();
    }

    private static void DoNotify(EventDef eventDef, CallbackInfo callbackInfo, Thing target, object data)
    {
        switch (callbackInfo.listener)
        {
            // ReSharper disable SuspiciousTypeConversion.Global
            case Thing { Destroyed: true }:
            case ThingComp t when t.parent.Destroyed:
            // ReSharper restore SuspiciousTypeConversion.Global
            case MapComponent m when m.map.Disposed:
                Log.Warning(
                    $"[EventManager] A destroyed thing got an event: {callbackInfo} ({eventDef.defName} on {target})");
                return;
        }

        if (doDebug)
            Debug.Log($"[EventManager]   {callbackInfo} (priority {callbackInfo.priority})");

        try
        {
            callbackInfo.wrappedCallback(target, data);
        }
        catch (Exception exception)
        {
            if (Prefs.DevMode)
            {
                Log.Error(
                    $"[EventManager] Exception notifying {callbackInfo} ({eventDef} on {target}): {exception}");
            }
            else if (callbackInfo.listener != null)
            {
                Log.ErrorOnce(
                    $"[EventManager] Exception notifying {callbackInfo} ({eventDef} on {target}). Suppressing further errors. Exception: {exception}",
                    callbackInfo.listener.GetHashCode() ^ 0x1c502196);
            }
        }
    }

    private void CallRegistrationHandlers(object thing)
    {
        if (thing is IEventListener target)
            AddListener(target);

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

        RegisterStaticListeners();

        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            Notify(EventDefOf.PostLoadedGame, pawn);
    }

    public override void FinalizeInit()
    {
        RegisterStaticListeners();
    }

    public static void AddStaticListener(IEventListener listener)
    {
        staticListeners.Add(listener);
        Instance?.RegisterStaticListeners();
    }

    private void RegisterStaticListeners()
    {
        foreach (var listener in staticListeners)
        {
            if (!alreadyRegisteredStaticListeners.Add(listener))
                continue;

            listener.RegisterWith(this);
        }
    }

    public void Reset()
    {
        registrations = new();
        for (int i = 0; i < notifications.Length; i++)
            notifications[i] = null;
        alreadyRegisteredStaticListeners.Clear();
    }

    public void AddListener(IEventListener listener)
    {
        listener.RegisterWith(this);
    }
}
