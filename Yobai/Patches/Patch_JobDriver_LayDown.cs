namespace Yobai.Patches;

[Patch(typeof(JobDriver_LayDown))]
internal static class Patch_JobDriver_LayDown
{
    [Postfix]
    [Target(nameof(JobDriver_LayDown.TryMakePreToilReservations))]
    public static void TryMakePreToilReservations_Postfix([Instance] JobDriver_LayDown driver, ref bool __result)
    {
        if (!__result || driver.Bed is not { } bed
                      || bed.GetComp<CompBedSharing>() is not { } comp)
            return;

        bool hasTemporaryAssignment = comp.TryGetSleepingSlot(driver.pawn, out _);
        if (!hasTemporaryAssignment && !comp.HasTemporaryAssignments)
            return;

        // An arriving owner must respect existing stays before its reservation can block them.
        // Temporary sleepers also need to remain eligible for shared beds.
        __result = (!hasTemporaryAssignment || BedSharingHelpers.CanSleepInSharedBed(driver.pawn, bed))
                   && RestUtility.IsValidBedFor(bed, driver.pawn, driver.pawn, true, guestStatus: driver.pawn.GuestStatus);
        if (!__result)
            driver.pawn.ClearReservationsForJob(driver.job);
    }
}
