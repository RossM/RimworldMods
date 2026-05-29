using XylXenos;

namespace XylXenos;

public class GeneSet(Pawn pawn) : INotificationListener
{
    public static readonly PawnTracker<GeneSet> Tracker = new(Make);
    public Pawn pawn = pawn;

    public float bodySizeFactor = 1f;
    public float healthScaleFactor = 1f;
    public float slaveRebellionThresholdDays = float.MaxValue;
    public float manhunterOnDamageChanceFactor = 1f;
    public float manhunterOnTameFailChanceFactor = 1f;
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;
    [CanBeNull] public List<BuildableDef> addDesignators;
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;
    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;
    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;
    public bool hasPsycast = false;

    private static GeneSet Make(Pawn pawn)
    {
        var geneSet = new GeneSet(pawn);
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