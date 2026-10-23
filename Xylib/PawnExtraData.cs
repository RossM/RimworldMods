using System.Collections.Concurrent;
using System.Threading;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Xylib;

/// <summary>
///     Represents a type of data that can be associated with a <see cref="Pawn" />. Implement this interface to create a
///     class that can be stored in <see cref="PawnExtraData{T}" />.
/// </summary>
[PublicAPI]
public interface IPawnData
{
    public Pawn Pawn { get; set; }

    /// <summary>
    ///     Called after the object is created or loaded to initialize it with the pawn it applies to.
    /// </summary>
    void Init();
}

/// <summary>
///     Provides one shared helper object per pawn for state that is important for the mod but not appropriate to keep in
///     a pawn <see cref="ThingComp" />, such as lookup caches or gene-derived aggregate values. Use <see cref="Get" /> to
///     retrieve a
///     pawn's instance.
/// </summary>
/// <remarks>
///     If the type <typeparamref name="T" /> implements <see cref="IExposable" />, it will be automatically saved and
///     loaded with the pawn.
/// </remarks>
public static class PawnExtraData<T> where T : IPawnData, new()
{
    private class Listener : IEventListener
    {
        private void Notify_InPawnExposeData(Thing? thing)
        {
            if (thing is not Pawn pawn)
                return;

            ExposeData(pawn);
        }

        private void Notify_PawnDiscarded(Thing? thing)
        {
            if (thing is not Pawn)
                return;

            lock (dataLock)
                data.TryRemove(thing.thingIDNumber, out _);
        }

        private void Notify_PostGameDispose()
        {
            lock (dataLock)
                data.Clear();
        }

        void IEventListener.RegisterWith(EventManager manager)
        {
            manager.Register(EventDefOf.PostDiscard, null, Notify_PawnDiscarded);
            manager.Register(EventDefOf.GlobalPostGameDispose, null, Notify_PostGameDispose);

            if (typeof(IExposable).IsAssignableFrom(typeof(T)))
                manager.Register(EventDefOf.InPawnExposeData, null, Notify_InPawnExposeData);
        }

        void IEventListener.PreUnregister(EventManager manager) { }
    }

    private static readonly ConcurrentDictionary<int, T> data = new();

    // Cached reads use ConcurrentDictionary without this lock. Mutations share it to prevent
    // duplicate initialization and to keep an in-flight Init from publishing after a removal,
    // clear, or load replacement has completed.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lock dataLock = new();

    private static readonly Listener listener = new();

    static PawnExtraData()
    {
        EventManager.AddStaticListener(listener);
    }

    /// <summary>
    ///     An XML tag to use when saving and loading this data. If the type <typeparamref name="T" /> has a
    ///     <see cref="ScribeLabelAttribute" />, its label will be used; otherwise, the full name of the type including
    ///     namespace will be used.
    /// </summary>
    public static string ScribeLabel { get; } = typeof(T).TryGetAttribute<ScribeLabelAttribute>()?.label ?? typeof(T).FullName!;

    /// <summary>
    ///     Retrieves the data associated with a <see cref="Pawn" />, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="pawn">The pawn to get the data for.</param>
    /// <returns>The data for the pawn.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get(Pawn pawn)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (data.TryGetValue(pawn.thingIDNumber, out var result))
            return result;

        return GetOrCreate(pawn);
    }

    private static T GetOrCreate(Pawn pawn)
    {
        // Init can have side effects, so only initialize once and publish the completed instance.
        lock (dataLock)
        {
            if (data.TryGetValue(pawn.thingIDNumber, out var result))
                return result;

            result = new T { Pawn = pawn };
            result.Init();
            data[pawn.thingIDNumber] = result;
            return result;
        }
    }

    // ExposeData won't be called in a multithreaded context
    private static void ExposeData(Pawn pawn)
    {
        lock (dataLock)
        {
            data.TryGetValue(pawn.thingIDNumber, out T? value);

            Scribe_Deep.Look(ref value, ScribeLabel);

            if (Scribe.mode != LoadSaveMode.LoadingVars)
                return;

            if (value == null)
            {
                data.TryRemove(pawn.thingIDNumber, out _);
            }
            else
            {
                value.Pawn = pawn;
                value.Init();
                data[pawn.thingIDNumber] = value;
            }
        }
    }
}
