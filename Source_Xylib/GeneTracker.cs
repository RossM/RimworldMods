namespace Xylib;

[PublicAPI]
public abstract class GeneTracker : IEventListener, IPawnData
{
    /// <summary>
    ///     The <see cref="Verse.Pawn" /> this object applies to.
    /// </summary>
    public Pawn Pawn
    {
        get => field ?? throw new InvalidOperationException();
        set;
    }

    void IPawnData.Init()
    {
        EventManager.Instance.AddListener(this);

        if (Scribe.mode == LoadSaveMode.Inactive)
            Update();
    }

    public abstract void Update();

    protected static void Append<T>(ref List<T>? dest, List<T>? source)
    {
        if (source == null || source.Count == 0)
            return;
        if (dest == null)
            dest = [.. source];
        else
            dest.AddRange(source);
    }

    protected static void Multiply<TItem, TKey>(ref Dictionary<TKey, float>? dest, List<TItem>? source, Func<TItem, TKey> keySelector, Func<TItem, float> valueSelector)
    {
        if (source is null)
            return;

        foreach (var item in source)
        {
            dest ??= [];
            var key = keySelector(item);
            var value = valueSelector(item);
            dest[key] = dest.GetValueOrDefault(key, 1f) * value;
        }
    }

    protected static void Add<TItem, TKey>(ref Dictionary<TKey, float>? dest, List<TItem>? source, Func<TItem, TKey> keySelector, Func<TItem, float> valueSelector)
    {
        if (source is null)
            return;

        foreach (var item in source)
        {
            dest ??= [];
            var key = keySelector(item);
            var value = valueSelector(item);
            dest[key] = dest.GetValueOrDefault(key, 0f) + value;
        }
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostLoadedGame, Pawn, Update);
        manager.Register(EventDefOf.PostGenesChanged, Pawn, Update);
        manager.Register(EventDefOf.PostMutated, Pawn, Update);
        manager.Register(EventDefOf.PostBirthday, Pawn, Update);
    }

    void IEventListener.PreUnregister(EventManager manager) { }
}
