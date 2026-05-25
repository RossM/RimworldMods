using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace XylXenos;

public class PawnTracker<T>(Func<Pawn, T> makeFunc) : INotificationListener
{
    private readonly Dictionary<int, T> data = new();

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
