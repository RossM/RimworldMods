namespace Yobai;

internal static class SharedSleepJobHelpers
{
    public static Job? TryMakeSleepJob(Pawn pawn, SleepingPlace place)
    {
        CompBedSharing? assignments = place.Bed.GetComp<CompBedSharing>();
        if (assignments == null || !BedSharingHelpers.CanSleepInSharedBed(pawn, place.Bed))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.LayDown, place.Bed);
        TemporarySleepingSlot? assignment = null;
        bool accepted = false;
        try
        {
            if (!pawn.Reserve(place.Bed, job, place.Bed.SleepingSlotsCount, 0, errorOnFailed: false))
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
