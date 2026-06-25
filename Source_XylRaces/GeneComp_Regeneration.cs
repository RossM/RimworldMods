namespace XylXenos;

public class GeneCompProperties_Regeneration : GeneCompProperties
{
    public float healthPerHour;

    public GeneCompProperties_Regeneration()
    {
        compClass = typeof(GeneComp_Regeneration);
    }
}

[UsedFromXml]
public class GeneComp_Regeneration : GeneComp
{
    public GeneCompProperties_Regeneration Props => (GeneCompProperties_Regeneration)props;
    private const int updateInterval = GenTicks.TicksPerRealSecond;

    [Unsaved] private List<Hediff_Injury> tmpHediffInjuries = [];

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(updateInterval, delta))
            return;

        if (!Pawn.health.hediffSet.HasNaturallyHealingInjury())
            return;

        Pawn.health.hediffSet.GetHediffs(ref tmpHediffInjuries, hediff => hediff.CanHealNaturally());

        float healingAmount = Props.healthPerHour * Pawn.HealthScale * updateInterval / GenDate.TicksPerHour /
                              tmpHediffInjuries.Count;
        foreach (var hediff in tmpHediffInjuries)
            hediff.Heal(healingAmount);
    }
}
