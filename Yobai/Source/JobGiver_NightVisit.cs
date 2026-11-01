namespace Yobai;

public sealed class JobGiver_NightVisit : ThinkNode_JobGiver
{
    private const int LovinChanceSeedSalt = 26504059;

    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!NightVisitHelpers.CanLeaveBed(pawn))
            return null;

        int now = Find.TickManager.TicksGame;
        if (pawn.mindState.thinkData.TryGetValue(UniqueSaveKey, out int lastTry) && now < lastTry + GenDate.TicksPerHour)
            return null;
        pawn.mindState.thinkData[UniqueSaveKey] = now;

        List<NightVisit> visits =
        [
            .. PolyculeHelpers.GetLovePartnersWillingToShareBed(pawn)
                // ReSharper disable once RedundantEnumerableCastCall
                .Select(partner => partner.CurrentBed()).OfType<Building_Bed>().Distinct()
                .SelectMany(bed => NightVisitHelpers.FindVisits(pawn, bed))
                .Where(visit => NightVisitHelpers.AreReadyForLovin(pawn, visit.Partner)),
        ];
        if (visits.Count == 0)
            return null;

        // Like vanilla's lovin' chance node, use a pawn-specific hourly roll without consuming
        // the global random stream. Choose the partner once, before rolling that pair's MTB.
        using var _ = new RandBlock(pawn.RandSeedForHour(Gen.HashCombineInt(UniqueSaveKey, LovinChanceSeedSalt)));
        NightVisit visit = visits.RandomElement()!;
        bool willing = Rand.MTBEventOccurs(LovePartnerRelationUtility.GetLovinMtbHours(pawn, visit.Partner),
            GenDate.TicksPerHour, GenDate.TicksPerHour);

        return willing ? NightVisitHelpers.TryMakeVisitJob(pawn, visit) : null;
    }
}
