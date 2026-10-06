namespace XylXenos;

[UsedFromXml]
public class HediffGiver_Parthenogenesis : HediffGiver_Random
{
    public override float ChanceFactor(Pawn pawn)
    {
        if (!pawn.ageTracker.CurLifeStage.reproductive)
            return 0f;

        // Pawns which are pregnant, sterilized, etc. can't get parthenogenetic pregnancy
        if (pawn.health.hediffSet.HasHediffPreventsPregnancy())
            return 0f;

        // Only apply the stat parts from Fertility, which cover age and hediffs. We deliberately ignore fertility
        // modifications from genes.
        float fertility = 1.0f;
        StatDefOf.Fertility.Worker.FinalizeValue(StatRequest.For(pawn), ref fertility, true);

        return fertility;
    }
}
