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
        TemporarySleepingSlot? assignment = null;
        CompBedSharing assignments = place.Bed.GetComp<CompBedSharing>();
        bool accepted = false;
        try
        {
            if (!pawn.Reserve(place.Bed, job, place.Bed.SleepingSlotsCount, 0))
                return null;
            assignment = assignments.TryAssignSleepingSlot(pawn, place.Slot);
            if (assignment == null
                || !RestUtility.IsValidBedFor(place.Bed, pawn, pawn, true, guestStatus: pawn.GuestStatus))
                return null;

            accepted = true;
            return job;
        }
        finally
        {
            // The assignment must exist for vanilla validation. A rejected job leaves no booking.
            if (!accepted)
            {
                if (assignment != null)
                    assignments.RemoveAssignment(assignment);
                pawn.ClearReservationsForJob(job);
                JobMaker.ReturnToPool(job);
            }
        }
    }
}
