namespace Yobai.Patches;

[Patch(typeof(Toils_Bed))]
internal static class Patch_Toils_Bed
{
    [Postfix]
    [Target(nameof(Toils_Bed.ClaimBedIfNonMedical))]
    public static void ClaimBedIfNonMedical_Postfix(TargetIndex ind, TargetIndex claimantIndex, Toil __result)
    {
        Action claimBed = __result.initAction;
        __result.initAction = () =>
        {
            Pawn actor = __result.actor;
            Pawn claimant = claimantIndex == TargetIndex.None
                ? actor : (Pawn)actor.CurJob.GetTarget(claimantIndex).Thing;
            Building_Bed bed = (Building_Bed)actor.CurJob.GetTarget(ind).Thing;
            // Automatic claims must preserve temporary stays. Manual assignments still work normally.
            if (bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(claimant, out _) != true)
                claimBed();
        };
    }
}
