namespace XylXenos;

public class GeneTracker : IEventListener, IPawnData
{
    /// <summary>
    ///     The <see cref="Pawn" /> this object applies to.
    /// </summary>
    public Pawn pawn;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.slaveRebellionThresholdDays" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.slaveRebellionThresholdDays" />
    /// </summary>
    public float slaveRebellionThresholdDays = float.MaxValue;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.joyGiverChanceFactors" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.joyGiverChanceFactors" />
    /// </summary>
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.addDesignators" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.addDesignators" />
    /// </summary>
    [CanBeNull] public List<BuildableDef> addDesignators;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.disableHostilityFromFactions" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.disableHostilityFromFactions" />
    /// </summary>
    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.ingestionThoughtOverrides" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.ingestionThoughtOverrides" />
    /// </summary>
    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

    // ReSharper disable once ParameterHidesMember
    public void Init(Pawn pawn)
    {
        this.pawn = pawn;
        EventManager.Instance.AddListener(this);
        Update();
    }

    public void Update()
    {
        slaveRebellionThresholdDays = float.MaxValue;
        addDesignators?.Clear();
        disableHostilityFromFactions?.Clear();
        ingestionThoughtOverrides?.Clear();

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneWithComps>())
            {
                var def = gene.DefExt;

                slaveRebellionThresholdDays = Mathf.Min(slaveRebellionThresholdDays, def.slaveRebellionThresholdDays);

                AddList(ref joyGiverChanceFactors, def.joyGiverChanceFactors);
                AddList(ref addDesignators, def.addDesignators);
                AddList(ref disableHostilityFromFactions, def.disableHostilityFromFactions);
                AddList(ref ingestionThoughtOverrides, def.ingestionThoughtOverrides);
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

    public void RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostGenesChanged, pawn, Notify_PostGenesChanged);
        manager.Register(EventDefOf.PostLoadedGame, pawn, Notify_PostLoadedGame);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
