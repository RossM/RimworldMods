namespace XylXenos;

public interface IPawnData
{
    void Init(Pawn pawn);
}

/// <summary>
///     Provides one shared helper object per pawn for state that is important for the mod but not appropriate to keep in
///     a pawn ThingComp, such as lookup caches or gene-derived aggregate values. Use <see cref="Get" /> to retrieve a
///     pawn's instance.
/// </summary>
public static class PawnDataManager<T> where T : IPawnData, new()
{
    private static readonly Dictionary<int, T> data = new();

    class Listener : INotificationListener
    {
        private void Notify_PawnDiscarded(Thing thing)
        {
            data.Remove(thing.thingIDNumber);
        }

        private void Notify_PostGameDispose()
        {
            data.Clear();
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationDefOf.PostDiscard, null, Notify_PawnDiscarded);
            manager.Register(NotificationDefOf.GlobalPostGameDispose, null, Notify_PostGameDispose);
        }

        public void PreUnregister(NotificationManager manager)
        {
        }
    }

    private static readonly Listener listener = new();

    static PawnDataManager()
    {
        NotificationManager.staticListeners.Add(listener);
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
