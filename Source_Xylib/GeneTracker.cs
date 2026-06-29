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

    public void Notify_PostGenesChanged()
    {
        Update();
    }

    public void Notify_PostLoadedGame()
    {
        Update();
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostGenesChanged, pawn, Notify_PostGenesChanged);
        manager.Register(EventDefOf.PostLoadedGame, pawn, Notify_PostLoadedGame);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
