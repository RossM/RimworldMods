namespace Xylib;

public abstract class GeneTracker : IEventListener, IPawnData
{
    /// <summary>
    ///     The <see cref="Pawn" /> this object applies to.
    /// </summary>
    public Pawn pawn;

    // ReSharper disable once ParameterHidesMember
    void IPawnData.Init(Pawn pawn)
    {
        this.pawn = pawn;
        EventManager.Instance.AddListener(this);

        if (Scribe.mode == LoadSaveMode.Inactive)
            Update();
    }

    public abstract void Update();

    protected void Append<T>(ref List<T> dest, List<T> source)
    {
        if (source.NullOrEmpty())
            return;
        if (dest == null)
            dest = [..source];
        else
            dest.AddRange(source);
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostLoadedGame, pawn, Update);
        manager.Register(EventDefOf.PostGenesChanged, pawn, Update);
        manager.Register(EventDefOf.PostMutated, pawn, Update);
        manager.Register(EventDefOf.PostBirthday, pawn, Update);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
