namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_LovinWithdrawal : ThoughtWorker_Hediff
{
    public override string PostProcessDescription(Pawn p, string description)
    {
        Hediff_LovinAddiction hediff = p.health.hediffSet.GetFirstHediffOfDef(def.hediff) as Hediff_LovinAddiction;
        Pawn partner = hediff?.sourcePawn;

        description = description.Formatted(p.Named("PAWN"), partner.Named("PARTNER"));
        if (hediff != null)
            description += $"\n\n{"CausedBy".Translate()}: {hediff.LabelBaseCap}";

        return description;
    }
}
