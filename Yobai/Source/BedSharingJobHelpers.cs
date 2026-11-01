namespace Yobai;

internal static class BedSharingJobHelpers
{
    public static Job? TryMakeSleepJob(Pawn pawn, SleepingPlace place)
    {
        if (!CanBookSleepingPlace(pawn, place))
            return null;

        if (!SleepingAssignments.TryAssignTemporary(pawn, place))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.LayDown, place.Bed);

        return job;
    }

    public static Job? TryMakeLovinJob(Pawn pawn, Pawn partner, SleepingPlace place)
    {
        if (!pawn.CanReserve(partner) || !CanBookSleepingPlace(pawn, place))
            return null;

        if (!SleepingAssignments.TryAssignTemporary(pawn, place))
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.Lovin, partner, place.Bed);

        return job;
    }

    public static bool CanBookSleepingPlace(Pawn pawn, SleepingPlace place)
    {
        return SleepingAssignments.CanAssignTemporary(pawn, place)
               && pawn.CanReserve(place.Bed, place.Bed.SleepingSlotsCount, 0);
    }
}
