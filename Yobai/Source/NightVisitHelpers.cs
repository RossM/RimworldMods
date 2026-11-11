namespace Yobai;

internal static class NightVisitHelpers
{
    extension(Pawn pawn)
    {
        public bool CanDoNightVisits
        {
            get
            {
                if (pawn.Awake() || pawn is not { CanShareBedsNow: true, CurJob.playerForced: false } || pawn.CurJobDef != JobDefOf.LayDown ||
                    pawn.CurrentBed() is not { Medical: false } bed)
                    return false;

                IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
                return !bedmates.Any(other => pawn != other && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other));
            }
        }
    }

    public static bool AreReadyForLovin(Pawn first, Pawn second)
    {
        int now = Find.TickManager.TicksGame;
        return now >= first.mindState!.canLovinTick &&
               now >= second.mindState!.canLovinTick &&
               first.health.capacities.CanBeAwake &&
               second.health.capacities.CanBeAwake &&
               first.CanReserve(second) &&
               second.CanReserve(first) &&
               LovePartnerRelationUtility.GetLovinMtbHours(first, second) > 0f;
    }

    public static SleepingPlace? GetFreeSlotInBed(Pawn visitor, Building_Bed bed)
    {
        if (bed.IsOwner(visitor, out int? assignedSlot) && assignedSlot is int ownedSlot && bed.GetSleepingPlace(ownedSlot).CanAssignForNightVisit(visitor))
            return bed.GetSleepingPlace(ownedSlot);
        
        for (int slot = 0; slot < bed.SleepingSlotsCount; slot++)
        {
            SleepingPlace place = bed.GetSleepingPlace(slot);
            if (place.CanAssignForNightVisit(visitor))
                return place;
        }

        return null;
    }

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
            if (!pawn.CanSleepInSharedBed(bed))
                continue;
            if (!bed.IsEligibleForNightVisits)
                continue;
            if (bed.GetComp<CompBedSharing>() is not { nightVisitsEnabled: true })
                continue;
            if (GetFreeSlotInBed(pawn, bed) is not { } place)
                continue;

            visits.Add(new(partner, place));
        }

        return visits;
    }
}
