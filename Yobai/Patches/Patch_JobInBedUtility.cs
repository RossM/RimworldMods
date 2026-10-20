namespace Yobai.Patches;

[Patch(typeof(JobInBedUtility))]
internal static class Patch_JobInBedUtility
{
    [Prefix]
    [Target(nameof(JobInBedUtility.KeepLyingDown))]
    public static bool KeepLyingDown_Prefix(JobDriver driver, TargetIndex bedIndex)
    {
        if (driver.job.def != JobDefOf.Lovin
            || driver.job.GetTarget(bedIndex).Thing is not Building_Bed bed
            || bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(driver.pawn, out _) != true
            || driver.pawn.Position == RestUtility.GetBedSleepingSlotPosFor(driver.pawn, bed))
            return true;

        // Vanilla starts lovin' in bed and always queues sleep there. A travelling visitor
        // must reach that bed before it becomes a sensible place to resume sleeping.
        driver.AddFinishAction(_ =>
        {
            Pawn pawn = driver.pawn;
            if (!pawn.Drafted && pawn.CurrentBed() == bed)
                pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.LayDown, bed));
        });
        return false;
    }
}
