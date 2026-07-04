namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_WithSource : ThoughtWorker_Hediff
{
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
