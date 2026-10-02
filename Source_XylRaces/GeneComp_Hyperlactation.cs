namespace XylXenos;

public class GeneCompProperties_Hyperlactation : GeneCompProperties
{
    public required HediffDef hediff;

    public GeneCompProperties_Hyperlactation()
    {
        compClass = typeof(GeneComp_Hyperlactation);
    }
}

public class GeneComp_Hyperlactation : GeneComp
{
    public GeneCompProperties_Hyperlactation Props => (GeneCompProperties_Hyperlactation)props;

    private const int CheckInterval = 60;

    public override void CompPostPostAdd()
    {
        AddHediff();
    }

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(CheckInterval, delta))
            return;

        AddHediff();
    }

    private void AddHediff()
    {
        if (!Active)
            return;

        if (Pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
            return;

        if (Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is { } lactatingHediff)
            Pawn.health.RemoveHediff(lactatingHediff);

        Hediff hediff = Pawn.health.GetOrAddHediff(Props.hediff);
        hediff.Severity = 1.0f;
    }
}
