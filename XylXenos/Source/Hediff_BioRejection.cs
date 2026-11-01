namespace XylXenos;

[UsedFromXml]
public class Hediff_BioRejection : HediffWithComps
{
    public override bool ShouldRemove => false;

    public override float Severity
    {
        get
        {
            DebugAssert.NotNull(pawn);

            return pawn.health.hediffSet.CountAddedAndImplantedParts() * 1.0f;
        }
        set { }
    }

    public override string Description
    {
        get
        {
            DebugAssert.NotNull(pawn);

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            List<string> causes =
            [
                .. hediffs.Where(hediff => hediff.def.countsAsAddedPartOrImplant).Select(hediff => hediff.Label),
            ];

            return $"{base.Description}\n\n{"CausedBy".Translate()}: {causes.ToCommaList().CapitalizeFirst()}";
        }
    }
}
