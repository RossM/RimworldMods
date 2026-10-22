namespace Yobai.Patches;

[Patch(typeof(JobGiver_GetRest))]
internal static class Patch_JobGiver_GetRest
{
    [Postfix]
    [Inner(typeof(RestUtility), nameof(RestUtility.FindBedFor), typeof(Pawn))]
    [Target("TryGiveJob")]
    public static void FindBedFor_Postfix(Pawn pawn, ref Building_Bed? __result)
    {
        if (__result is { Medical: true } || !BedSharingHelpers.CanChooseSharedBed(pawn))
            return;

        // Reuse the booking while travelling; another AI check must not reshuffle it.
        if (pawn.CurJobDef == JobDefOf.LayDown
            && pawn.CurJob.targetA.Thing is Building_Bed destination
            && BedSharingHelpers.CanUseTemporarySleepingSlot(pawn, destination))
            __result = destination;
        else if (pawn.CurrentBed() == null && BedSelectionHelpers.FindPolyculeBed(pawn) is { } bed)
            __result = bed;
    }
}
