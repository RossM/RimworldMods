namespace Yobai;

internal static class NightVisitHelpers
{
    public static bool CanLeaveBed(Pawn pawn)
    {
        if (pawn.Awake() || pawn is not { CanShareBedsNow: true, CurJob.playerForced: false } || pawn.CurJobDef != JobDefOf.LayDown ||
            pawn.CurrentBed() is not { Medical: false } bed)
            return false;

        IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
        return !bedmates.Any(other => pawn != other && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other));
    }

    public static bool AreReadyForLovin(Pawn first, Pawn second)
    {
        int now = Find.TickManager.TicksGame;
        return LovePartnerRelationUtility.LovePartnerRelationExists(first, second)
               && now >= first.mindState.canLovinTick && now >= second.mindState.canLovinTick
               && first.health.capacities.CanBeAwake && second.health.capacities.CanBeAwake
               && first.CanReserve(second) && second.CanReserve(first)
               && LovePartnerRelationUtility.GetLovinMtbHours(first, second) > 0f;
    }

    public static IEnumerable<NightVisit> FindVisits(Pawn visitor, Building_Bed bed)
    {
        if (!visitor.CanShareBedsNow
            || !visitor.CanSleepInSharedBed(bed)
            || !bed.IsEligibleForNightVisits
            || bed.GetComp<CompBedSharing>() is not { nightVisitsEnabled: true } comp)
            yield break;

        if (!comp.GetBedmates().Any(other => BedSharingHelpers.AreLovePartnersWillingToShareBed(visitor, other)))
            yield break;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        if (bed.IsOwner(visitor, out int? assignedSlot) && assignedSlot is int ownedSlot)
            slots = slots.Prepend(ownedSlot).Distinct();
        int slot = slots.FirstOrDefault(slot => bed.GetSleepingPlace(slot).CanAssignForNightVisit(visitor), -1);
        if (slot < 0)
            yield break;

        SleepingPlace place = bed.GetSleepingPlace(slot);
        foreach (Pawn partner in bed.CurOccupants)
        {
            if (BedSharingHelpers.AreLovePartnersWillingToShareBed(visitor, partner) && !partner.Awake() && partner.CanShareBedsNow)
                yield return new NightVisit(partner, place);
        }
    }

    public static Job? TryMakeVisitJob(Pawn visitor, Building_Bed bed) =>
        FindVisits(visitor, bed).FirstOrDefault()?.TryMakeVisitJob(visitor);
}
