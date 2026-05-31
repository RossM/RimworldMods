namespace XylXenos;

/// <summary>
///     Provides one shared helper object per pawn for state that is important for the mod but not appropriate to keep in
///     a pawn ThingComp, such as lookup caches or gene-derived aggregate values. Use <see cref="Get" /> to retrieve a
///     pawn's instance.
/// </summary>
public class PawnDataManager<T> : INotificationListener
{
    private readonly Dictionary<int, T> data = new();
    private readonly Func<Pawn, T> makeFunc;

    public PawnDataManager(Func<Pawn, T> makeFunc)
    {
        this.makeFunc = makeFunc;
        NotificationManager.staticListeners.Add(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(Pawn pawn)
    {
        if (!data.TryGetValue(pawn.thingIDNumber, out T result))
        {
            result = makeFunc(pawn);
            data.Add(pawn.thingIDNumber, result);
        }

        return result;
    }

    private void Notify_PostGameDispose()
    {
        data.Clear();
    }

    private void Notify_PawnDiscarded(Thing thing)
    {
        data.Remove(thing.thingIDNumber);
    }

    public void RegisterWith(NotificationManager manager)
    {
        manager.Register(NotificationEvent.PostDiscard, null, Notify_PawnDiscarded);
        manager.Register(NotificationEvent.PostGameDispose, null, Notify_PostGameDispose);
    }
}
