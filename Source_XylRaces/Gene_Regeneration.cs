namespace XylXenos;

public class RegenerationProperties : GeneProperties
{
    public float healthPerHour;
}

[UsedFromXml]
public class Gene_Regeneration : GeneExt
{
    public RegenerationProperties Props => (RegenerationProperties)DefExt.props;
    private const int updateInterval = GenTicks.TicksPerRealSecond;

    [Unsaved] private List<Hediff_Injury> tmpHediffInjuries = [];

    public override void TickInterval(int delta)
    {
        if (!pawn.IsHashIntervalTick(updateInterval, delta))
            return;

        if (!pawn.health.hediffSet.HasNaturallyHealingInjury())
            return;

        pawn.health.hediffSet.GetHediffs(ref tmpHediffInjuries, hediff => hediff.CanHealNaturally());

        float healingAmount = Props.healthPerHour * pawn.HealthScale * updateInterval / GenDate.TicksPerHour /
                              tmpHediffInjuries.Count;
        foreach (var hediff in tmpHediffInjuries)
            hediff.Heal(healingAmount);
    }
}
