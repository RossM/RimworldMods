namespace Xylib;

public class GeneTracker : IEventListener, IPawnData
{
    /// <summary>
    ///     The <see cref="Pawn" /> this object applies to.
    /// </summary>
    public Pawn pawn;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.bodySizeFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.bodySizeFactor" />
    /// </summary>
    public float bodySizeFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.healthScaleFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.healthScaleFactor" />
    /// </summary>
    public float healthScaleFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.hasPsycast" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.hasPsycast" />
    /// </summary>
    public bool hasPsycast = false;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.renderNodeModifiers" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.renderNodeModifiers" />
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;

    // ReSharper disable once ParameterHidesMember
    void IPawnData.Init(Pawn pawn)
    {
        this.pawn = pawn;
        EventManager.Instance.AddListener(this);
        Update();
    }

    public void Update()
    {
        bodySizeFactor = 1f;
        healthScaleFactor = 1f;
        renderNodeModifiers?.Clear();
        hasPsycast = false;

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneWithComps>())
            {
                var def = gene.DefExt;

                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;
                hasPsycast |= def.hasPsycast;

                AddList(ref renderNodeModifiers, def.renderNodeModifiers);
            }
        }
    }

    private void AddList<T>(ref List<T> dest, List<T> source)
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
