namespace Yobai.Patches;

[Patch(typeof(JobGiver_GetRest))]
internal static class Patch_JobGiver_GetRest
{
    [Postfix]
    [Inner(typeof(RestUtility), nameof(RestUtility.FindBedFor), typeof(Pawn))]
    [Target("TryGiveJob")]
    public static void FindBedFor_Postfix(Pawn pawn, ref Building_Bed? __result, [State] out Job? sleepJob)
    {
        sleepJob = null;
        if (__result is { Medical: true } || !BedSharingHelpers.CanChooseSharedBed(pawn))
            return;

        // Reuse the booking while travelling; another AI check must not reshuffle it.
        if (pawn.CurJobDef == JobDefOf.LayDown
            && pawn.CurJob.targetA.Thing is Building_Bed destination
            && BedSharingHelpers.CanUseTemporarySleepingSlot(pawn, destination))
            sleepJob = pawn.CurJob;
        else if (pawn.CurrentBed() == null)
            sleepJob = BedSelectionHelpers.ChooseBedtimeSleepJob(pawn);

        if (sleepJob != null)
            __result = (Building_Bed)sleepJob.targetA.Thing;
    }

    [Prefix]
    [Inner(typeof(JobMaker), nameof(JobMaker.MakeJob), typeof(JobDef), typeof(LocalTargetInfo))]
    [Target("TryGiveJob")]
    public static bool MakeJob_Prefix([State] Job? sleepJob, out Job? __result)
    {
        __result = sleepJob;
        return sleepJob == null;
    }
}
