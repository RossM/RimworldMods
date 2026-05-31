namespace XylXenos;

public class GeneTracker(Pawn pawn) : INotificationListener
{
    /// <summary>
    ///     A tracker used to find the object associated with a specific <see cref="Pawn" />.
    /// </summary>
    public static readonly PawnDataManager<GeneTracker> DataManager = new(Make);

    /// <summary>
    ///     The <see cref="Pawn" /> this object applies to.
    /// </summary>
    public Pawn pawn = pawn;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.bodySizeFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.bodySizeFactor" />
    /// </summary>
    public float bodySizeFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.healthScaleFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.healthScaleFactor" />
    /// </summary>
    public float healthScaleFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.slaveRebellionThresholdDays" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.slaveRebellionThresholdDays" />
    /// </summary>
    public float slaveRebellionThresholdDays = float.MaxValue;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.manhunterOnDamageChanceFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.manhunterOnDamageChanceFactor" />
    /// </summary>
    public float manhunterOnDamageChanceFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.manhunterOnTameFailChanceFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.manhunterOnTameFailChanceFactor" />
    /// </summary>
    public float manhunterOnTameFailChanceFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.hasPsycast" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.hasPsycast" />
    /// </summary>
    public bool hasPsycast = false;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.joyGiverChanceFactors" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.joyGiverChanceFactors" />
    /// </summary>
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.addDesignators" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.addDesignators" />
    /// </summary>
    [CanBeNull] public List<BuildableDef> addDesignators;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.renderNodeModifiers" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.renderNodeModifiers" />
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.disableHostilityFromFactions" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.disableHostilityFromFactions" />
    /// </summary>
    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_Gene.ingestionThoughtOverrides" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_Gene.ingestionThoughtOverrides" />
    /// </summary>
    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

    private static GeneTracker Make(Pawn pawn)
    {
        var geneSet = new GeneTracker(pawn);
        geneSet.RegisterWith(NotificationManager.Instance);
        geneSet.Update();
        return geneSet;
    }

    public void Update()
    {
        bodySizeFactor = 1f;
        healthScaleFactor = 1f;
        slaveRebellionThresholdDays = float.MaxValue;
        manhunterOnDamageChanceFactor = 1f;
        manhunterOnTameFailChanceFactor = 1f;
        joyGiverChanceFactors?.Clear();
        addDesignators?.Clear();
        renderNodeModifiers?.Clear();
        disableHostilityFromFactions?.Clear();
        ingestionThoughtOverrides?.Clear();
        hasPsycast = false;

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneExt>())
            {
                var def = gene.DefExt;

                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;
                slaveRebellionThresholdDays = Mathf.Min(slaveRebellionThresholdDays, def.slaveRebellionThresholdDays);
                manhunterOnDamageChanceFactor *= def.manhunterOnDamageChanceFactor;
                manhunterOnTameFailChanceFactor *= def.manhunterOnTameFailChanceFactor;

                AddList(ref joyGiverChanceFactors, def.joyGiverChanceFactors);
                AddList(ref addDesignators, def.addDesignators);
                AddList(ref renderNodeModifiers, def.renderNodeModifiers);
                AddList(ref disableHostilityFromFactions, def.disableHostilityFromFactions);
                AddList(ref ingestionThoughtOverrides, def.ingestionThoughtOverrides);

                hasPsycast |= def.hasPsycast;
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

    public void RegisterWith(NotificationManager manager)
    {
        manager.Register(NotificationEvent.PostGenesChanged, pawn, Notify_PostGenesChanged);
        manager.Register(NotificationEvent.PostLoadedGame, pawn, Notify_PostLoadedGame);
    }
}
