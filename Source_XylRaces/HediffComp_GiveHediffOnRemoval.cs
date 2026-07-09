using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_GiveHediffOnRemoval : HediffCompProperties
{
    public required HediffDef hediff;

    public bool inheritSeverity;

    public HediffCompProperties_GiveHediffOnRemoval()
    {
        compClass = typeof(HediffComp_GiveHediffOnRemoval);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (hediff is null)
            yield return $"{nameof(hediff)} is null";
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

    public void ApplyTo(Pawn pawn)
    {
        Hediff newHediff = HediffMaker.MakeHediff(partRecord: parent.Part, def: Props.hediff, pawn: pawn);

        if (Props.inheritSeverity)
            newHediff.Severity = parent.Severity;

        pawn.health.AddHediff(newHediff);
    }
}
