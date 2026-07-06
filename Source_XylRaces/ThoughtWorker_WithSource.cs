namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_WithSource : ThoughtWorker_Hediff
{
    public override string PostProcessLabel(Pawn p, string label)
    {
        HediffWithCompsExt hediff = p.health.hediffSet.GetFirstHediffOfDef(def.hediff) as HediffWithCompsExt;
        Pawn sourcePawn = hediff?.sourcePawn;

        label = label.Formatted(p.Named("PAWN"), sourcePawn.Named("SOURCE"));

        return label;
    }

    public override string PostProcessDescription(Pawn p, string description)
    {
        HediffWithCompsExt hediff = p.health.hediffSet.GetFirstHediffOfDef(def.hediff) as HediffWithCompsExt;
        Pawn sourcePawn = hediff?.sourcePawn;

        description = description.Formatted(p.Named("PAWN"), sourcePawn.Named("SOURCE"));
        if (hediff != null)
            description += $"\n\n{"CausedBy".Translate()}: {hediff.LabelBaseCap}";

        return description;
    }
}
