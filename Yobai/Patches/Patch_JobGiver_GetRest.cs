namespace Yobai.Patches;

[Patch(typeof(JobGiver_GetRest))]
internal static class Patch_JobGiver_GetRest
{
    [Postfix]
    [Target("TryGiveJob")]
    public static void TryGiveJob_Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result?.def != JobDefOf.LayDown || __result.targetA.Thing is Building_Bed { Medical: true }
            || !BedSharingHelpers.CanChooseSharedBed(pawn))
            return;

        // Keep the chosen destination when the AI checks for another job on the way to bed.
        if (pawn.CurJobDef == JobDefOf.LayDown
            && pawn.CurJob.targetA.Thing is Building_Bed destination
            && BedSharingHelpers.CanUseTemporarySleepingSlot(pawn, destination))
        {
            JobMaker.ReturnToPool(__result);
            __result = pawn.CurJob;
            return;
        }
        if (pawn.CurrentBed() != null)
            return;

        Job? sleep = BedSelectionHelpers.ChooseBedtimeSleepJob(pawn);
        if (sleep != null)
        {
            JobMaker.ReturnToPool(__result);
            __result = sleep;
        }
    }
}
