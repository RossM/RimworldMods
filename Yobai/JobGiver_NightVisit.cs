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

        List<NightVisit> visits = PolyculeHelpers.GetBedSharingPartners(pawn)
            .Select(partner => partner.CurrentBed()).OfType<Building_Bed>().Distinct()
            .SelectMany(bed => NightVisitHelpers.FindVisits(pawn, bed))
            .Where(visit => NightVisitHelpers.AreReadyForLovin(pawn, visit.Partner)).ToList();
        if (visits.Count == 0)
            return null;

        // Like vanilla's lovin' chance node, use a pawn-specific hourly roll without consuming
        // the global random stream. Choose the partner once, before rolling that pair's MTB.
        Rand.PushState(pawn.RandSeedForHour(Gen.HashCombineInt(UniqueSaveKey, LovinChanceSeedSalt)));
        NightVisit visit;
        bool willing;
        try
        {
            visit = visits.RandomElement();
            willing = Rand.MTBEventOccurs(LovePartnerRelationUtility.GetLovinMtbHours(pawn, visit.Partner),
                GenDate.TicksPerHour, GenDate.TicksPerHour);
        }
        finally
        {
            Rand.PopState();
        }
        return willing ? NightVisitHelpers.TryMakeVisitJob(pawn, visit) : null;
    }
}
