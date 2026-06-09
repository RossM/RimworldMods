using RimWorld.Planet;

namespace XylXenos;

[UsedFromXml]
public class HediffGiver_RandomExt : HediffGiver
{
    public float mtbDays;
    public bool sendLetter = true;
    public bool allowDuplicates = false;
    public bool inheritSeverity = false;
    public FloatRange severityRange = FloatRange.Zero;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        float num = mtbDays;
        float num2 = ChanceFactor(pawn);
        if (num2 != 0f && Rand.MTBEventOccurs(num / num2, GenDate.TicksPerDay, 60f) && TryApply(pawn, cause))
        {
            if (sendLetter)
                SendLetter(pawn, cause);
        }
    }

    public bool TryApply(Pawn pawn, Hediff cause)
    {
        if (!allowOnLodgers && pawn.IsQuestLodger())
            return false;
        if (!allowOnQuestRewardPawns && pawn.IsWorldPawn() && pawn.IsQuestReward())
            return false;
        if (!allowOnQuestReservedPawns && pawn.IsWorldPawn() && Find.WorldPawns.GetSituation(pawn) == WorldPawnSituation.ReservedByQuest)
            return false;
        if (ModsConfig.IdeologyActive && !allowOnBeggars && pawn.kindDef == PawnKindDefOf.Beggar)
            return false;
        if (pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby && Find.Storyteller.difficulty.babiesAreHealthy)
            return false;
        if (pawn.genes != null && !pawn.genes.HediffGiversCanGive(hediff))
            return false;
        if (pawn.IsMutant && !pawn.mutant.HediffGiversCanGive(hediff))
            return false;

        return TryApplyInner(pawn, cause);
    }

    private bool TryApplyInner(Pawn pawn, Hediff cause)
    {
        if (canAffectAnyLivePart || partsToAffect != null)
        {
            bool result = false;
            for (int i = 0; i < countToAffect; i++)
            {
                IEnumerable<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts();
                if (partsToAffect != null)
                    parts = parts.Where(p => partsToAffect.Contains(p.def));
                if (canAffectAnyLivePart)
                    parts = parts.Where(p => p.def.alive);
                if (!allowDuplicates)
                    parts = parts.Where(p => !pawn.health.hediffSet.HasHediff(hediff, p));
                parts = parts.Where(p => !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(p));

                parts = parts.ToList();
                if (!parts.Any())
                    break;

                Hediff newHediff = HediffMaker.MakeHediff(partRecord: parts.RandomElementByWeight(x => x.coverageAbs), def: hediff,
                    pawn: pawn);

                if (inheritSeverity)
                    newHediff.Severity = cause.Severity;
                else if (severityRange != FloatRange.Zero)
                    newHediff.Severity = severityRange.RandomInRange;

                pawn.health.AddHediff(newHediff);
                result = true;
            }

            return result;
        }
        else
        {
            if (!allowDuplicates && pawn.health.hediffSet.HasHediff(hediff))
                return false;

            Hediff newHediff = HediffMaker.MakeHediff(hediff, pawn);

            if (inheritSeverity)
                newHediff.Severity = cause.Severity;
            else if (severityRange != FloatRange.Zero)
                newHediff.Severity = severityRange.RandomInRange;

            pawn.health.AddHediff(newHediff);
            return true;
        }
    }
}
