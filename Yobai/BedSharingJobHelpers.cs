namespace Yobai;

internal static class BedSharingJobHelpers
{
    public static Job? TryMakeSleepJob(Pawn pawn, SleepingPlace place)
    {
        if (!CanBookSleepingPlace(pawn, place))
            return null;

        return TryBookSleepingPlace(pawn, place, JobMaker.MakeJob(JobDefOf.LayDown, place.Bed));
    }

    public static Job? TryMakeLovinJob(Pawn pawn, Pawn partner, SleepingPlace place)
    {
        if (!pawn.CanReserve(partner) || !CanBookSleepingPlace(pawn, place))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.Lovin, partner, place.Bed);
        if (!pawn.Reserve(partner, job))
        {
            JobMaker.ReturnToPool(job);
            return null;
        }

        return TryBookSleepingPlace(pawn, place, job);
    }

    private static bool CanBookSleepingPlace(Pawn pawn, SleepingPlace place)
    {
        return BedSharingHelpers.CanSleepInSharedBed(pawn, place.Bed)
               && place.Bed.GetComp<CompBedSharing>()?.CanAssignSleepingSlot(pawn, place.Slot) == true
               && pawn.CanReserve(place.Bed, place.Bed.SleepingSlotsCount, 0);
    }

    private static Job? TryBookSleepingPlace(Pawn pawn, SleepingPlace place, Job job)
    {
        if (!pawn.Reserve(place.Bed, job, place.Bed.SleepingSlotsCount, 0))
            return null;

        if (!TryAssignSleepingSlot(pawn, place))
        {
            pawn.ClearReservationsForJob(job);
            JobMaker.ReturnToPool(job);
            return null;
        }

        return job;
    }

    private static bool TryAssignSleepingSlot(Pawn pawn, SleepingPlace place)
    {
        CompBedSharing assignments = place.Bed.GetComp<CompBedSharing>();
        TemporarySleepingSlot? assignment = assignments.TryAssignSleepingSlot(pawn, place.Slot);
        if (assignment == null)
            return false;
        if (!RestUtility.IsValidBedFor(place.Bed, pawn, pawn, true, guestStatus: pawn.GuestStatus))
        {
            assignments.RemoveAssignment(assignment);
            return false;
        }

        return true;
    }
}
