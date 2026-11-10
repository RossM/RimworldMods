namespace Yobai;

internal static class BedSharingJobHelpers
{
    public static Job? TryMakeSleepJob(Pawn pawn, SleepingPlace place)
    {
        if (!place.BookableBy(pawn))
            return null;

        place.AssignTemporary(pawn);

        Job job = JobMaker.MakeJob(JobDefOf.LayDown, place.Bed);

        return job;
    }

    public static Job? TryMakeLovinJob(Pawn pawn, Pawn partner, SleepingPlace place)
    {
        if (!pawn.CanReserve(partner) || !place.BookableBy(pawn))
            return null;

        place.AssignTemporary(pawn);

        Job job = JobMaker.MakeJob(JobDefOf.Lovin, partner, place.Bed);

        return job;
    }
}
