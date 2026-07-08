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

    // ReSharper disable once ParameterHidesMember
    void IPawnData.Init()
    {
        EventManager.Instance.AddListener(this);

        if (Scribe.mode == LoadSaveMode.Inactive)
            Update();
    }

    public abstract void Update();

    protected void Append<T>(ref List<T>? dest, List<T>? source)
    {
        if (source == null || source.Count == 0)
            return;
        if (dest == null)
            dest = [..source];
        else
            dest.AddRange(source);
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostLoadedGame, Pawn, Update);
        manager.Register(EventDefOf.PostGenesChanged, Pawn, Update);
        manager.Register(EventDefOf.PostMutated, Pawn, Update);
        manager.Register(EventDefOf.PostBirthday, Pawn, Update);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
