namespace Xylib;

public interface IPawnData
{
    void Init(Pawn pawn);
}

/// <summary>
///     Provides one shared helper object per pawn for state that is important for the mod but not appropriate to keep in
///     a pawn <see cref="ThingComp" />, such as lookup caches or gene-derived aggregate values. Use <see cref="Get" /> to
///     retrieve a
///     pawn's instance.
/// </summary>
public static class PawnExtraData<T> where T : IPawnData, new()
{
    private class Listener : IEventListener
    {
        private void Notify_InPawnExposeData(Thing thing)
        {
            if (thing is not Pawn pawn)
                return;

            ExposeData(pawn);
        }

        private void Notify_PawnDiscarded(Thing thing)
        {
            data.Remove(thing.thingIDNumber);
        }

        private void Notify_PostGameDispose()
        {
            data.Clear();
        }

        void IEventListener.RegisterWith(EventManager manager)
        {
            manager.Register(EventDefOf.PostDiscard, null, Notify_PawnDiscarded);
            manager.Register(EventDefOf.GlobalPostGameDispose, null, Notify_PostGameDispose);
    
            if (typeof(IExposable).IsAssignableFrom(typeof(T)))
                manager.Register(EventDefOf.InPawnExposeData, null, Notify_InPawnExposeData);
        }

        void IEventListener.PreUnregister(EventManager manager)
        {
        }
    }

    private static readonly Dictionary<int, T> data = new();

    private static readonly Listener listener = new();

    static PawnExtraData()
    {
        EventManager.AddStaticListener(listener);
    }

    public static string ScribeLabel { get; } = typeof(T).TryGetAttribute<ScribeLabelAttribute>()?.label ?? typeof(T).FullName;

    /// <summary>
    ///     Retrieves the data associated with a <see cref="Pawn" />, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="pawn">The pawn to get the data for.</param>
    /// <returns>The data for the pawn.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get(Pawn pawn)
    {
        if (!data.TryGetValue(pawn.thingIDNumber, out T result))
        {
            result = new T();
            result.Init(pawn);
            data.Add(pawn.thingIDNumber, result);
        }

        return result;
    }

    public static void Set(Pawn pawn, T value)
    {
        data[pawn.thingIDNumber] = value;
    }

    private static void ExposeData(Pawn pawn)
    {
        if (!data.TryGetValue(pawn.thingIDNumber, out T value))
            value = default(T);

        Scribe_Deep.Look(ref value, ScribeLabel);

        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;

        if (value == null)
            data.Remove(pawn.thingIDNumber);
        else
        {
            value.Init(pawn);
            data[pawn.thingIDNumber] = value;
        }
    }
}
