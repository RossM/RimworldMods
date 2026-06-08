namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_WithPartner : ThoughtWorker_Hediff
{
    public override string PostProcessDescription(Pawn p, string description)
    {
        HediffWithCompsExt hediff = p.health.hediffSet.GetFirstHediffOfDef(def.hediff) as HediffWithCompsExt;
        Pawn partner = hediff?.sourcePawn;
        Log.Message($"hediff={hediff} partner={partner}");

        description = description.Formatted(p.Named("PAWN"), partner.Named("PARTNER"));
        if (hediff != null)
            description += $"\n\n{"CausedBy".Translate()}: {hediff.LabelBaseCap}";

        return description;
    }
}
