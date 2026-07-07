using RimWorld.Planet;

namespace Xylib;

/// <summary>
///     This is an enhanced version of <see cref="HediffGiver_Random" /> that adds additional features.
/// </summary>
[UsedFromXml]
public class HediffGiver_RandomExt : HediffGiver
{
    /// <summary>
    ///     The average number of days between consecutive triggers. The actual interval is random.
    /// </summary>
    public float mtbDays;

    /// <summary>
    ///     Whether to send a letter when the hediff is applied. Defaults to true.
    /// </summary>
    public bool sendLetter = true;

    /// <summary>
    ///     Whether to allow applying the same hediff multiple times to the same body part. Defaults to false.
    /// </summary>
    /// <remarks>
    ///     Most hediffs will stack and combine severity if applied multiple times.
    /// </remarks>
    public bool allowDuplicates = false;

    /// <summary>
    ///     When triggered from another hediff, whether to inherit the severity of the cause hediff. If false, the severity
    ///     will be set to a random value in <see cref="severityRange" />.
    /// </summary>
    public bool inheritSeverity = false;

    /// <summary>
    ///     The severity range to use when applying the hediff. The actual value will be a random value from the range.
    /// </summary>
    public FloatRange severityRange = FloatRange.Zero;

    /// <summary>
    ///     Called every 60 ticks (1 real-time second).
    /// </summary>
    /// <param name="pawn">The pawn the hediff should be applied to, if triggered.</param>
    /// <param name="cause">The hediff that is a parent to this object, if any.</param>
    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        float chanceFactor = ChanceFactor(pawn);
        if (chanceFactor != 0f && Rand.MTBEventOccurs(mtbDays / chanceFactor, GenDate.TicksPerDay, 60f) && TryApply(pawn, cause))
        {
            if (sendLetter)
                SendLetter(pawn, cause);
        }
    }

    /// <summary>
    ///     Called when the hediff giver is triggered.
    /// </summary>
    /// <param name="pawn">The pawn the hediff should be applied to.</param>
    /// <param name="cause">The hediff that is a parent to this object, if any.</param>
    /// <returns></returns>
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
