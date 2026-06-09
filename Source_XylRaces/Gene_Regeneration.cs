namespace XylXenos;

public class RegenerationInfo
{
    public float healthPerHour;
}

[UsedFromXml]
public class Gene_Regeneration : GeneExt
{
    public RegenerationInfo RegenerationInfo => DefExt.regeneration;
    private const int updateInterval = GenTicks.TicksPerRealSecond;

    [Unsaved] private List<Hediff_Injury> tmpHediffInjuries = [];

    public override void TickInterval(int delta)
    {
        if (!pawn.IsHashIntervalTick(updateInterval, delta))
            return;

        if (!pawn.health.hediffSet.HasNaturallyHealingInjury())
            return;

        pawn.health.hediffSet.GetHediffs(ref tmpHediffInjuries, hediff => hediff.CanHealNaturally());

        float healingAmount = RegenerationInfo.healthPerHour * pawn.HealthScale * updateInterval / GenDate.TicksPerHour /
                              tmpHediffInjuries.Count;
        foreach (var hediff in tmpHediffInjuries)
            hediff.Heal(healingAmount);
    }
}
