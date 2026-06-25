namespace Xylib;

public interface IPawnData
{
    void Init(Pawn pawn);
}

/// <summary>
///     Provides one shared helper object per pawn for state that is important for the mod but not appropriate to keep in
///     a pawn ThingComp, such as lookup caches or gene-derived aggregate values. Use <see cref="Get" /> to retrieve a
///     pawn's instance.
/// </summary>
public static class PawnExtraData<T> where T : IPawnData, new()
{
    private class Listener : IEventListener
    {
        private void Notify_PawnDiscarded(Thing thing)
        {
            data.Remove(thing.thingIDNumber);
        }

        private void Notify_PostGameDispose()
        {
            data.Clear();
        }

        public void RegisterWith(EventManager manager)
        {
            manager.Register(EventDefOf.PostDiscard, null, Notify_PawnDiscarded);
            manager.Register(EventDefOf.GlobalPostGameDispose, null, Notify_PostGameDispose);
        }

        public void PreUnregister(EventManager manager)
        {
        }
    }

    private static readonly Dictionary<int, T> data = new();

    private static readonly Listener listener = new();

    static PawnExtraData()
    {
        EventManager.staticListeners.Add(listener);
    }

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
}
