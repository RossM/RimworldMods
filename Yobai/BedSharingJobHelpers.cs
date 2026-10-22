namespace Yobai;

internal static class BedSharingJobHelpers
{
    public static Job? TryMakeSleepJob(Pawn pawn, SleepingPlace place)
    {
        if (!CanBookSleepingPlace(pawn, place))
            return null;

        if (!TryAssignSleepingSlot(pawn, place))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.LayDown, place.Bed);

        return job;
    }

    public static Job? TryMakeLovinJob(Pawn pawn, Pawn partner, SleepingPlace place)
    {
        if (!pawn.CanReserve(partner) || !CanBookSleepingPlace(pawn, place))
            return null;

        if (!TryAssignSleepingSlot(pawn, place))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.Lovin, partner, place.Bed);

        return job;
    }

    public static bool CanBookSleepingPlace(Pawn pawn, SleepingPlace place)
    {
        return BedSharingHelpers.CanSleepInSharedBed(pawn, place.Bed)
               && place.Bed.GetComp<CompBedSharing>()?.CanAssignSleepingSlot(pawn, place.Slot) == true
               && pawn.CanReserve(place.Bed, place.Bed.SleepingSlotsCount, 0);
    }

    public static bool TryAssignSleepingSlot(Pawn pawn, SleepingPlace place)
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
