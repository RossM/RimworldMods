namespace Xylib;

/// <summary>
///     Provides pawn-generation context to early generation callbacks.
/// </summary>
/// <param name="request">
///     The request being used to generate the pawn.
/// </param>
/// <param name="xenotype">
///     The xenotype chosen for the pawn.
/// </param>
public class PawnGenerationData(PawnGenerationRequest request, XenotypeDef xenotype)
{
    /// <summary>
    ///     The request being used to generate the pawn.
    /// </summary>
    public PawnGenerationRequest request = request;

    /// <summary>
    ///     The xenotype chosen for the pawn.
    /// </summary>
    public XenotypeDef xenotype = xenotype;
}

/// <summary>
///     Implemented by objects that register callbacks with <see cref="EventManager" />.
/// </summary>
public interface IEventListener
{
    /// <summary>
    ///     Called when a listener is created or loaded. The listener should call
    ///     <see cref="O:EventManager.Register" />
    ///     to register for any events it should receive.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="EventManager" /> that should be registered with. This is always
    ///     <see cref="EventManager.Instance" />.
    /// </param>
    public void RegisterWith(EventManager manager);

    /// <summary>
    ///     Called before a listener is removed from the <see cref="EventManager" />. Events registered directly by the
    ///     listener will be removed automatically, but child objects such as comps should be removed with
    ///     <see cref="EventManager.RemoveListener" />.
    /// </summary>
    /// <param name="manager">
    ///     The <see cref="EventManager" /> that should be unregistered with. This is always
    ///     <see cref="EventManager.Instance" />.
    /// </param>
    public void PreUnregister(EventManager manager);
}

/// <summary>
///     Defines an event that can be raised with <see cref="EventManager.Notify" />.
/// </summary>
public class EventDef : Def
{
    /// <summary>
    ///     Whether the event is raised without a target.
    /// </summary>
    public bool global = false;

    /// <summary>
    ///     The type of data passed to callbacks, or null if the event does not pass data.
    /// </summary>
    public Type dataType = null;

    /// <summary>
    ///     Whether the event can be raised while RimWorld is saving or loading data.
    /// </summary>
    public bool allowDuringScribe = false;
}

/// <summary>
///     Contains the built-in event definitions used by Xylib's Harmony patches.
/// </summary>
[DefOf]
public static class EventDefOf
{
    /// <summary>
    ///     Called after <see cref="Game.Dispose" />, immediately before the event manager unregisters all listeners.
    ///     This hook is raised without a target.
    /// </summary>
    public static EventDef GlobalPostGameDispose;

    /// <summary>
    ///     Called inside <see cref="StartingPawnUtility.GeneratePossessions" />.
    ///     This hook passes a <see cref="List&lt;ThingDefCount&gt;" /> as the "data" parameter.
    /// </summary>
    public static EventDef InGeneratePossessions;

    /// <summary>
    ///     Called in <see cref="Pawn.ExposeData" />.
    /// </summary>
    public static EventDef InPawnExposeData;

    /// <summary>
    ///     Called after <see cref="Pawn_ApparelTracker.Notify_ApparelChanged" />.
    /// </summary>
    public static EventDef PostApparelChanged;

    /// <summary>
    ///     Called after <c>Pawn_AgeTracker.BirthdayBiological</c>.
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
    ///     Called after <c>Pawn_HealthTracker.MakeDowned</c>.
    /// </summary>
    public static EventDef PostDowned;

    /// <summary>
    ///     Called after <c>PawnGenerator.GenerateInitialHediffs</c>.
    /// </summary>
    public static EventDef PostGenerateInitialHediffs;

    /// <summary>
    ///     Called after <c>PawnGenerator.GenerateNewPawnInternal</c>.
    ///     This hook passes a <see cref="PawnGenerationRequest" /> as its "data" parameter.
    /// </summary>
    public static EventDef PostGenerateNewPawn;

    /// <summary>
    ///     Called after <c>Pawn_GeneTracker.Notify_GenesChanged</c>.
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
    ///     Called after <see cref="Pawn_JobTracker.StartJob" /> successfully starts a job.
    ///     This hook passes a <see cref="JobDriver" /> as its "data" parameter.
    /// </summary>
    public static EventDef PostJobStarted;

    /// <summary>
    ///     Called after <see cref="EventManager.LoadedGame" />, immediately after the event manager
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
    ///     Called inside <c>PawnGenerator.TryGenerateNewPawnInternal</c> before the <see cref="Pawn" />'s bio and name
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
///     Allows listeners to register for global or target-specific callbacks raised by patched game hooks.
/// </summary>
/// <para>
///     Implement <see cref="IEventListener" /> and register for events in
///     <see cref="IEventListener.RegisterWith" />.
/// </para>
/// <para>
///     Subtypes of the following classes that implement <see cref="IEventListener" /> will have
///     <see cref="IEventListener.RegisterWith" /> called automatically:
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Gene" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Hediff" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="HediffComp" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="MapComponent" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Need" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Thing" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ThingComp" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="GeneComp" />
///             </description>
///         </item>
///     </list>
///     For other types, you should call <see cref="AddListener" /> when the listener should start receiving events, and
///     <see cref="RemoveListener" /> when the listener should stop receiving events.
/// </para>
[StaticConstructorOnStartup]
public class EventManager
{
    // Holds data about all the callbacks for a specific EventDef.
    private class NotificationInfo
    {
        public bool usesPriority = false;
        public readonly List<CallbackInfo> globalCallbacks = [];
        public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
    }

    // Holds data about a registered callback, used when unregistering a listener.
    private class RegistrationInfo(EventDef eventDef, Thing target)
    {
        public readonly EventDef eventDef = eventDef;
        public readonly bool isGlobal = target == null;
        public readonly System.WeakReference<Thing> target = target == null ? null : new(target);
    }

    // Holds data about a specific callback that should be called when an event is triggered.
    private struct CallbackInfo
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

    private static bool doDebug = false;

    // Listeners that are automatically registered when a game is started or loaded.
    private static readonly List<IEventListener> staticListeners = [];

    // Static listeners which are already registered, to avoid double registration.
    [Unsaved] private readonly HashSet<IEventListener> alreadyRegisteredStaticListeners = [];

    // Information about the listeners registered for each event.
    private NotificationInfo[] notifications;

    // Events that each listener has registered for, used for unregistering the listener.
    private ConditionalWeakTable<IEventListener, List<RegistrationInfo>> registrations = new();

    // Scratch list used during event handling.
    private readonly List<CallbackInfo> tempCallbacks = [];

    /// <summary>
    ///     Gets the shared event manager instance.
    /// </summary>
    [NotNull]
    public static EventManager Instance { get; } = new();

    /// <summary>
    ///     Toggles development logging for event registration and notification.
    /// </summary>
    [DebugAction(allowedGameStates = 0)]
    public static void ToggleNotificationManagerLogging()
    {
        doDebug = !doDebug;
    }

    private void Init()
    {
        notifications ??= new NotificationInfo[DefDatabase<EventDef>.DefCount];
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
        {
            Debug.Log(
                $"[EventManager] Register eventDef={eventDef} {(target == null ? "global" : $"target=[{target}]")} listener={listener} name={name} priority={priority}");
        }

        Init();

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
                    $"[EventManager] Adding a duplicate callback: eventDef={eventDef} global listener={listener} name={name}");
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
                    $"[EventManager] Adding a duplicate callback: eventDef={eventDef} target=[{target}] listener={listener} name={name}");
                return;
            }

            localCallbacks.Add(callbackInfo);
        }
    }

    /// <summary>
    ///     Registers a callback for an event, passing both the event target and typed event data to the callback.
    /// </summary>
    /// <param name="eventDef">
    ///     The event to listen for.
    /// </param>
    /// <param name="target">
    ///     If non-null, the callback will only be invoked if the event target matches. If null, the callback will be invoked
    ///     for all targets.
    /// </param>
    /// <param name="callback">
    ///     The callback to invoke. Its target object must implement <see cref="IEventListener" />.
    /// </param>
    /// <param name="priority">
    ///     Optional callback priority. Higher-priority callbacks run first.
    /// </param>
    /// <typeparam name="T">
    ///     The expected type of the event data.
    /// </typeparam>
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

    /// <summary>
    ///     Registers a callback for an event, passing typed event data to the callback.
    /// </summary>
    /// <param name="eventDef">
    ///     The event to listen for.
    /// </param>
    /// <param name="target">
    ///     If non-null, the callback will only be invoked if the event target matches. If null, the callback will be invoked
    ///     for all targets.
    /// </param>
    /// <param name="callback">
    ///     The callback to invoke. Its target object must implement <see cref="IEventListener" />.
    /// </param>
    /// <param name="priority">
    ///     Optional callback priority. Higher-priority callbacks run first.
    /// </param>
    /// <typeparam name="T">
    ///     The expected type of the event data.
    /// </typeparam>
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

    /// <summary>
    ///     Registers a callback for an event, passing the event target to the callback.
    /// </summary>
    /// <param name="eventDef">
    ///     The event to listen for.
    /// </param>
    /// <param name="target">
    ///     If non-null, the callback will only be invoked if the event target matches. If null, the callback will be invoked
    ///     for all targets.
    /// </param>
    /// <param name="callback">
    ///     The callback to invoke. Its target object must implement <see cref="IEventListener" />.
    /// </param>
    /// <param name="priority">
    ///     Optional callback priority. Higher-priority callbacks run first.
    /// </param>
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

    /// <summary>
    ///     Registers a callback for an event without passing event target or data arguments to the callback.
    /// </summary>
    /// <param name="eventDef">
    ///     The event to listen for.
    /// </param>
    /// <param name="target">
    ///     If non-null, the callback will only be invoked if the event target matches. If null, the callback will be invoked
    ///     for all targets.
    /// </param>
    /// <param name="callback">
    ///     The callback to invoke. Its target object must implement <see cref="IEventListener" />.
    /// </param>
    /// <param name="priority">
    ///     Optional callback priority. Higher-priority callbacks run first.
    /// </param>
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

    /// <summary>
    ///     Registers a callback owned by the supplied listener, passing both the event target and typed event data to the
    ///     callback.
    /// </summary>
    /// <param name="listener">
    ///     The listener that owns the registration and will be used for later unregistration.
    /// </param>
    /// <param name="eventDef">
    ///     The event to listen for.
    /// </param>
    /// <param name="target">
    ///     If non-null, the callback will only be invoked if the event target matches. If null, the callback will be invoked
    ///     for all targets.
    /// </param>
    /// <param name="callback">
    ///     The callback to invoke.
    /// </param>
    /// <typeparam name="T">
    ///     The expected type of the event data.
    /// </typeparam>
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

    /// <summary>
    ///     Unregisters all callbacks owned by a listener.
    /// </summary>
    /// <param name="listener">
    ///     The listener to remove.
    /// </param>
    public void RemoveListener(IEventListener listener)
    {
        Init();

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

                if (notifications[record.eventDef.index]?.localCallbacks?.TryGetValue(target, out List<CallbackInfo> callbacks) is true)
                    callbacks.RemoveAll(callback => callback.listener == listener);
            }
        }

        registrations.Remove(listener);
    }

    /// <summary>
    ///     Raises an event for a target, invoking global callbacks and callbacks registered specifically for that target.
    /// </summary>
    /// <param name="eventDef">
    ///     The event to raise.
    /// </param>
    /// <param name="target">
    ///     The target object the event applies to, or null for a global event.
    /// </param>
    /// <param name="data">
    ///     Optional event data. When supplied, its type should match <see cref="EventDef.dataType" />.
    /// </param>
    public void Notify(EventDef eventDef, Thing target, object data = null)
    {
        if (Scribe.mode != LoadSaveMode.Inactive && !eventDef.allowDuringScribe)
            return;

        if (doDebug)
            Debug.Log($"[EventManager] Notify eventDef={eventDef} target=[{target}] data={data}");

        if (Prefs.DevMode)
            ValidateNotifyArgs(eventDef, target, data);

        Init();

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

    private static void ValidateNotifyArgs(EventDef eventDef, Thing target, object data)
    {
        if (eventDef.global)
        {
            if (target != null)
            {
                Log.ErrorOnce($"[EventManager] Notification {eventDef.defName} is global but was called with target {target}",
                    Gen.HashCombineInt(0x34330AEF, eventDef.index));
            }
        }
        else
        {
            if (target == null)
            {
                Log.ErrorOnce($"[EventManager] Notification {eventDef.defName} is not global but was called with null target",
                    Gen.HashCombineInt(0x140A0CA2, eventDef.index));
            }
        }

        if (eventDef.dataType != null)
        {
            if (data == null)
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} should take data of type {eventDef.dataType} but was given null",
                    Gen.HashCombineInt(0xEEB8AC2, eventDef.index));
            }
            else if (!eventDef.dataType.IsAssignableFrom(data.GetType()))
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} should take data of type {eventDef.dataType} but was given {data.GetType()}",
                    Gen.HashCombineInt(0x4D53041B, eventDef.index));
            }
        }
        else
        {
            if (data != null)
            {
                Log.ErrorOnce(
                    $"[EventManager] Notification {eventDef.defName} shouldn't take data but was given {data.GetType()}",
                    Gen.HashCombineInt(0x7A213146, eventDef.index));
            }
        }
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

    /// <summary>
    ///     Called after a saved game is loaded.
    /// </summary>
    public void LoadedGame()
    {
        using var _ = new ProfileBlock();

        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            CallRegistrationHandlers(pawn);

        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            Notify(EventDefOf.PostLoadedGame, pawn);
    }

    /// <summary>
    ///     Adds a listener that is registered for every game load and reset.
    /// </summary>
    /// <param name="listener">
    ///     The listener to add.
    /// </param>
    public static void AddStaticListener(IEventListener listener)
    {
        staticListeners.Add(listener);
        Instance.RegisterStaticListeners();
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

    /// <summary>
    ///     Clears all current registrations and re-registers static listeners.
    /// </summary>
    public void Reset()
    {
        Init();

        registrations = new();
        for (int i = 0; i < notifications.Length; i++)
            notifications[i] = null;
        alreadyRegisteredStaticListeners.Clear();
        RegisterStaticListeners();
    }

    /// <summary>
    ///     Registers a listener by calling <see cref="IEventListener.RegisterWith" />.
    /// </summary>
    /// <param name="listener">
    ///     The listener to register.
    /// </param>
    public void AddListener(IEventListener listener)
    {
        listener.RegisterWith(this);
    }
}
