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
               && now >= first.mindState!.canLovinTick && now >= second.mindState!.canLovinTick
               && first.health.capacities.CanBeAwake && second.health.capacities.CanBeAwake
               && first.CanReserve(second) && second.CanReserve(first)
               && LovePartnerRelationUtility.GetLovinMtbHours(first, second) > 0f;
    }

    public static IEnumerable<NightVisit> FindVisits(Pawn visitor, Building_Bed bed)
    {
        if (!BedIsEligibleForNightVisits(visitor, bed))
            yield break;

        var comp = bed.GetComp<CompBedSharing>()!;

        if (!comp.GetBedmates().Any(other => BedSharingHelpers.AreLovePartnersWillingToShareBed(visitor, other)))
            yield break;

        if (GetFreeSlotInBed(visitor, bed) is not { } place)
            yield break;

        foreach (Pawn partner in bed.CurOccupants)
        {
            if (BedSharingHelpers.AreLovePartnersWillingToShareBed(visitor, partner) && !partner.Awake() && partner.CanShareBedsNow)
                yield return new NightVisit(partner, place);
        }
    }

    public static SleepingPlace? GetFreeSlotInBed(Pawn visitor, Building_Bed bed)
    {
        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        if (bed.IsOwner(visitor, out int? assignedSlot) && assignedSlot is int ownedSlot)
            slots = slots.Prepend(ownedSlot).Distinct();
        int slot = slots.FirstOrDefault(slot => bed.GetSleepingPlace(slot).CanAssignForNightVisit(visitor), -1);
        return slot >= 0 ? bed.GetSleepingPlace(slot) : null;
    }

    public static bool BedIsEligibleForNightVisits(Pawn visitor, Building_Bed bed) =>
        (visitor.CanShareBedsNow
         && visitor.CanSleepInSharedBed(bed)
         && bed.IsEligibleForNightVisits
         && bed.GetComp<CompBedSharing>() is { nightVisitsEnabled: true });

    public static Job? TryMakeVisitJob(Pawn visitor, Building_Bed bed) =>
        // GetPotentialNightVisits(visitor).FirstOrDefault(visit => visit.Place.Bed == bed)?.TryMakeVisitJob(visitor);
        FindVisits(visitor, bed).FirstOrDefault()?.TryMakeVisitJob(visitor);

    public static List<NightVisit> GetPotentialNightVisits(Pawn pawn)
    {
        List<NightVisit> visits = [];
        foreach (var partner in BedSharingHelpers.GetLovePartnersWillingToShareBed(pawn))
        {
            if (partner.Awake())
                continue;
            if (!partner.CanShareBedsNow)
                continue;
            if (!AreReadyForLovin(pawn, partner))
                continue;
            if (partner.CurrentBed() is not { } bed)
                continue;
            if (!BedIsEligibleForNightVisits(pawn, bed))
                continue;
            if (GetFreeSlotInBed(pawn, bed) is not { } place)
                continue;

            visits.Add(new(partner, place));
        }

        return visits;
    }
}
