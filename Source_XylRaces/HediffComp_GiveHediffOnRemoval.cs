namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_GiveHediffOnRemoval : HediffCompProperties
{
    public HediffDef hediff;

    public bool inheritSeverity;

    public HediffCompProperties_GiveHediffOnRemoval()
    {
        compClass = typeof(HediffComp_GiveHediffOnRemoval);
    }
}

public class HediffComp_GiveHediffOnRemoval : HediffComp
{
    public HediffCompProperties_GiveHediffOnRemoval Props => (HediffCompProperties_GiveHediffOnRemoval)props;

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();

        ApplyTo(parent.pawn);
    }

    public bool ApplyTo(Pawn pawn)
    {
        Hediff hediff2 = HediffMaker.MakeHediff(partRecord: parent.Part, def: Props.hediff, pawn: pawn);

        if (Props.inheritSeverity)
            hediff2.Severity = parent.Severity;

        pawn.health.AddHediff(hediff2);
        return true;
    }
}
