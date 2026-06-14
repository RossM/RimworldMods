namespace XylXenos;

public class PawnGenerationEarlyData(PawnGenerationRequest request, XenotypeDef xenotype)
{
    public PawnGenerationRequest request = request;
    public XenotypeDef xenotype = xenotype;
}

public interface INotificationListener
{
    /// <summary>
    ///     Called when a listener is created or loaded. The listener should call
    ///     <see cref="O:XylXenos.NotificationManager.Register" />
    ///     to register for any notifications they want to receive.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="NotificationManager" /> that should be registered with. This is always
    ///     <see cref="NotificationManager.Instance" />.
    /// </param>
    public void RegisterWith(NotificationManager manager);

    /// <summary>
    ///     Called when a listener is about to be removed from the notification manager. Notifications registered by the
    ///     listener directly
    ///     will be removed automatically, but if there are any child objects such as Comps that need to be unregistered, call
    ///     <see cref="NotificationManager.UnregisterAll" /> for each one.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="NotificationManager" /> that should be unregistered with. This is always
    ///     <see cref="NotificationManager.Instance" />.
    /// </param>
    public void PreUnregister(NotificationManager manager);
}

public class NotificationDef : Def
{
    public bool global = false;
    public Type dataType = null;
}

[RimWorld.DefOf]
public static class NotificationDefOf
{
    /// <summary>
    ///     Called after <see cref="Game.Dispose" />, immediately before the notification manager unregisters all listeners.
    ///     This hook passes null as its "pawn" parameter, so can only be used as a global hook.
    /// </summary>
    public static NotificationDef GlobalPostGameDispose;

    /// <summary>
    ///     Called inside <see cref="PawnGenerator.TryGenerateNewPawnInternal" /> before the <see cref="Pawn" />'s bio and name
    ///     are generated.
    ///     This hook can be used to modify the pawn's <see cref="Gender" /> and <see cref="XenotypeDef" /> during generation.
    ///     This hook passes a <see cref="PawnGenerationEarlyData" /> as the "data" parameter.
    /// </summary>
    public static NotificationDef PawnGenerationEarly;

    /// <summary>
    ///     Called after <see cref="Pawn_ApparelTracker.Notify_ApparelChanged" />.
    /// </summary>
    public static NotificationDef PostApparelChanged;

    /// <summary>
    ///     Called after <see cref="Pawn_HealthTracker.CheckForStateChange" />.
    /// </summary>
    public static NotificationDef PostCheckForStateChange;

    /// <summary>
    ///     Called after <see cref="Thing.Discard" />.
    /// </summary>
    public static NotificationDef PostDiscard;

    /// <summary>
    ///     Called after <see cref="Pawn_HealthTracker.MakeDowned" />.
    /// </summary>
    public static NotificationDef PostDowned;

    /// <summary>
    ///     Called after <see cref="PawnGenerator.GenerateNewPawnInternal" />.
    ///     This hook passes a <see cref="PawnGenerationRequest" /> as its "data" parameter.
    /// </summary>
    public static NotificationDef PostGenerateNewPawn;

    /// <summary>
    ///     Called after <see cref="Pawn_GeneTracker.Notify_GenesChanged" />.
    /// </summary>
    public static NotificationDef PostGenesChanged;

    // PostHediffsChanged and PostHediffStateChange are generally both called when a pawn's hediffs change, but
    // PostHediffsChanged is called as soon as the HediffSet is updated, while PostHediffStateChange is called
    // slightly later when the game is checking for the results of the changes. I'm not sure if having both is
    // really necessary but out of caution I'm leaving them both here for now.

    /// <summary>
    ///     Called after <see cref="HediffSet.DirtyCache" />.
    /// </summary>
    public static NotificationDef PostHediffsChanged;

    /// <summary>
    ///     Called after <see cref="NotificationManager.LoadedGame" />, immediately after the notification manager
    ///     has called <see cref="INotificationListener.RegisterWith" /> on all listeners.
    /// </summary>
    public static NotificationDef PostLoadedGame;

    /// <summary>
    ///     Called after a pawn does lovin'.
    ///     This hook passes a <see cref="Pawn" /> as its "data" parameter.
    /// </summary>
    public static NotificationDef PostLovin;


    /// <summary>
    ///     Called after <see cref="Pawn.Kill(Verse.DamageInfo?,Hediff)" />.
    /// </summary>
    public static NotificationDef PostPawnKilled;

    /// <summary>
    ///     Called after <see cref="Thing.PostMake" />.
    /// </summary>
    public static NotificationDef PostPostMake;

    /// <summary>
    ///     Called after <see cref="PawnGenerator.RedressPawn" />.
    ///     This hook passes a <see cref="PawnGenerationRequest" /> as its "data" parameter.
    /// </summary>
    public static NotificationDef PostRedressPawn;

    /// <summary>
    ///     Called after <see cref="GeneUtility.SatisfyChemicalGenes" />.
    /// </summary>
    public static NotificationDef PostSatisfyChemicalGenes;

    /// <summary>
    ///     Called before <see cref="Thing.TakeDamage" />.
    ///     This hook passes a <see cref="DamageInfo" /> as its "data" parameter.
    /// </summary>
    public static NotificationDef PreTakeDamage;

    static NotificationDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(NotificationDefOf));
    }
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
    private class NotificationInfo
    {
        public bool usesPriority = false;
        public readonly List<CallbackInfo> globalCallbacks = [];
        public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
    }

    private class RegistrationInfo(NotificationDef notification, Thing target)
    {
        public readonly NotificationDef notification = notification;
        public readonly bool isGlobal = target == null;
        public readonly System.WeakReference<Thing> target = target == null ? null : new(target);
    }

    public struct CallbackInfo
    {
        public Action<Thing, object> wrappedCallback;
        public INotificationListener listener;
        public string name;
        public int priority;

        public override string ToString()
        {
            return $"{name}[{listener}]";
        }
    }

    public static NotificationManager Instance => Current.Game.GetComponent<NotificationManager>();
    private static bool doDebug = false;

    public static List<INotificationListener> staticListeners = [];
    [Unsaved] public HashSet<INotificationListener> alreadyRegisteredStaticListeners = [];

    private readonly NotificationInfo[] notifications = new NotificationInfo[DefDatabase<NotificationDef>.DefCount];

    private ConditionalWeakTable<INotificationListener, List<RegistrationInfo>> registrations = new();

    private readonly List<CallbackInfo> tempCallbacks = [];

    public NotificationManager(Game _)
    {
    }

    [DebugAction(allowedGameStates = 0)]
    public static void ToggleNotificationManagerLogging()
    {
        doDebug = !doDebug;
    }

    private void RegisterInternal(
        NotificationDef notification,
        Thing target,
        Action<Thing, object> callback,
        object source,
        string name,
        int priority)
    {
        if (source is not INotificationListener listener)
            throw new InvalidOperationException("Only an INotificationListener can register for notifications");

        if (doDebug)
            Debug.Log(
                $"NotificationManager: Register notification={notification} {(target == null ? "global" : $"target=[{target}]")} listener={listener} name={name} priority={priority}");

        var records = registrations.GetOrCreateValue(listener);
        records.Add(new(notification, target));

        NotificationInfo notificationInfo = notifications[notification.index] ??= new();
        if (priority != 0)
            notificationInfo.usesPriority = true;

        CallbackInfo callbackInfo = new() { wrappedCallback = callback, listener = listener, name = name, priority = priority };

        if (target == null)
        {
            if (notificationInfo.globalCallbacks.Any(c => c.listener == listener && c.name == name))
            {
                Log.Warning(
                    $"NotificationManager: Adding a duplicate callback: type={notification} global listener={listener} name={name}");
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
                    $"NotificationManager: Adding a duplicate callback: type={notification} target={target} listener={listener} name={name}");
                return;
            }

            localCallbacks.Add(callbackInfo);
        }
    }

    public void Register<T>(NotificationDef notification, Thing target, [NotNull] Action<Thing, T> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (notification == null)
        {
            Log.WarningOnce("NotificationManager: eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        if (!typeof(T).IsAssignableFrom(notification.dataType))
        {
            Log.ErrorOnce(
                $"NotificationManager: Registered callback for {callback.Target.GetType()} {notification.defName} expects {typeof(T)} but event will pass {notification.dataType}",
                Gen.HashCombineInt(0x467A56FF, notification.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(notification, target, (t, data) => callback(t, (T)data), callback.Target, MethodName(callback), priority);
    }

    public void Register<T>(NotificationDef notification, Thing target, [NotNull] Action<T> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (notification == null)
        {
            Log.WarningOnce("NotificationManager: eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        if (!typeof(T).IsAssignableFrom(notification.dataType))
        {
            Log.ErrorOnce(
                $"NotificationManager: Registered callback for {callback.Target.GetType()} {notification.defName} expects {typeof(T)} but event will pass {notification.dataType}",
                Gen.HashCombineInt(0x467A56FF, notification.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(notification, target, (_, data) => callback((T)data), callback.Target, MethodName(callback), priority);
    }

    // ReSharper disable once UnusedMember.Global
    public void Register(NotificationDef notification, Thing target, [NotNull] Action<Thing> callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (notification == null)
        {
            Log.WarningOnce("NotificationManager: eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        RegisterInternal(notification, target, (t, _) => callback(t), callback.Target, MethodName(callback), priority);
    }

    public void Register(NotificationDef notification, Thing target, [NotNull] Action callback, int priority = 0)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (notification == null)
        {
            Log.WarningOnce("NotificationManager: eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, callback.Target?.GetHashCode() ?? 0));
            return;
        }

        RegisterInternal(notification, target, (_, _) => callback(), callback.Target, MethodName(callback), priority);
    }

    private static string MethodName(Delegate fn)
    {
        return $"{fn.Method.DeclaringType?.Name ?? "<global>"}.{fn.Method.Name}";
    }

    // ReSharper disable once UnusedMember.Global
    public void Register<T>(
        [NotNull] INotificationListener listener,
        NotificationDef notification,
        Thing target,
        [NotNull] Action<Thing, T> callback)
    {
        if (listener == null)
            throw new ArgumentNullException(nameof(listener));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (notification == null)
        {
            Log.WarningOnce("NotificationManager: eventType is null - are you missing a NotificationDef in XML?",
                Gen.HashCombineInt(0x7AEC159, listener.GetHashCode()));
            return;
        }

        if (!typeof(T).IsAssignableFrom(notification.dataType))
        {
            Log.ErrorOnce(
                $"NotificationManager: Registered callback for {callback.Target.GetType()} {notification.defName} expects {typeof(T)} but event will pass {notification.dataType}",
                Gen.HashCombineInt(0x467A56FF, notification.index, callback.Target.GetType().GetHashCode(), 0));
        }

        RegisterInternal(notification, target, (t, data) => callback(t, (T)data), listener, $"{listener.GetType().Name}.<{notification.defName}>", 0);
    }

    public void UnregisterAll(INotificationListener listener)
    {
        listener.PreUnregister(this);

        if (!registrations.TryGetValue(listener, out List<RegistrationInfo> records))
            return;

        foreach (var record in records)
        {
            if (record.isGlobal)
                notifications[record.notification.index]?.globalCallbacks.RemoveAll(callback => callback.listener == listener);
            else
            {
                if (!record.target.TryGetTarget(out Thing target))
                    continue;

                if (notifications[record.notification.index]?.localCallbacks?.TryGetValue(target, out List<CallbackInfo> callbacks) == true)
                    callbacks.RemoveAll(callback => callback.listener == listener);
            }
        }

        registrations.Remove(listener);
    }

    public void Notify(NotificationDef notification, Thing target, object data = null)
    {
        if (Scribe.mode != LoadSaveMode.Inactive)
            return;

        if (doDebug)
            Debug.Log($"NotificationManager: Notify notification={notification} target=[{target}] data={data}");

        if (Prefs.DevMode)
        {
            if (notification.global && target != null)
            {
                Log.ErrorOnce($"NotificationManager: Notification {notification.defName} is global but was called with target {target}",
                    Gen.HashCombineInt(0x34330AEF, notification.index));
            }

            if (!notification.global && target == null)
            {
                Log.ErrorOnce($"NotificationManager: Notification {notification.defName} is not global but was called with null target",
                    Gen.HashCombineInt(0x140A0CA2, notification.index));
            }

            if (notification.dataType != null && data == null)
            {
                Log.ErrorOnce(
                    $"NotificationManager: Notification {notification.defName} should take data of type {notification.dataType} but was given null",
                    Gen.HashCombineInt(0xEEB8AC2, notification.index));
            }

            if (notification.dataType != null && data != null && !notification.dataType.IsAssignableFrom(data.GetType()))
            {
                Log.ErrorOnce(
                    $"NotificationManager: Notification {notification.defName} should take data of type {notification.dataType} but was given {data.GetType()}",
                    Gen.HashCombineInt(0x4D53041B, notification.index));
            }

            if (notification.dataType == null && data != null)
            {
                Log.ErrorOnce(
                    $"NotificationManager: Notification {notification.defName} shouldn't take data but was given {data.GetType()}",
                    Gen.HashCombineInt(0x7A213146, notification.index));
            }
        }

        NotificationInfo notificationInfo = notifications[notification.index];
        if (notificationInfo == null)
            return;

        List<CallbackInfo> localCallbacks;

        if (!notificationInfo.usesPriority)
        {
            foreach (CallbackInfo callbackInfo in notificationInfo.globalCallbacks)
            {
                DoNotify(notification, callbackInfo, target, data);
            }

            if (target == null || !notificationInfo.localCallbacks.TryGetValue(target, out localCallbacks))
                return;

            foreach (CallbackInfo callbackInfo in localCallbacks)
            {
                DoNotify(notification, callbackInfo, target, data);
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
            DoNotify(notification, callbackInfo, target, data);
        }

        tempCallbacks.Clear();
    }

    private static void DoNotify(NotificationDef notification, CallbackInfo callbackInfo, Thing target, object data)
    {
        switch (callbackInfo.listener)
        {
            // ReSharper disable SuspiciousTypeConversion.Global
            case Thing { Destroyed: true }:
            case ThingComp t when t.parent.Destroyed:
            // ReSharper restore SuspiciousTypeConversion.Global
            case MapComponent m when m.map.Disposed:
            case GeneExt { Removed: true }:
                Log.Warning(
                    $"NotificationManager: A destroyed thing got an event: {callbackInfo} ({notification.defName} on {target})");
                return;
        }

        if (doDebug)
            Debug.Log($"NotificationManager:   {callbackInfo} (priority {callbackInfo.priority})");

        try
        {
            callbackInfo.wrappedCallback(target, data);
        }
        catch (Exception exception)
        {
            if (Prefs.DevMode)
            {
                Log.Error(
                    $"NotificationManager: Exception notifying {callbackInfo} ({notification} on {target}): {exception}");
            }
            else if (callbackInfo.listener != null)
            {
                Log.ErrorOnce(
                    $"NotificationManager: Exception notifying {callbackInfo} ({notification} on {target}). Suppressing further errors. Exception: {exception}",
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
            Notify(NotificationDefOf.PostLoadedGame, pawn);
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
        registrations = new();
        for (int i = 0; i < notifications.Length; i++)
            notifications[i] = null;
        alreadyRegisteredStaticListeners.Clear();
    }
}
