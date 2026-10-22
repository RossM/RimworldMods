namespace Yobai.Patches;

[Patch(typeof(RestUtility))]
internal static class Patch_RestUtility
{
    [Postfix]
    [Target(nameof(RestUtility.CanUseBedNow))]
    public static void CanUseBedNow_Postfix(Thing bedThing, Pawn sleeper, ref bool __result)
    {
        if (!__result || bedThing is not Building_Bed bed)
            return;
        CompBedSharing? comp = bed.GetComp<CompBedSharing>();
        if (comp?.HasTemporaryAssignments != true)
            return;

        // Permanent ownership grants a place, but cannot make unrelated occupants bedmates.
        if (!BedSharingHelpers.AllBedmatesWillingToShareBed(sleeper, bed))
        {
            __result = false;
            return;
        }

        // Permanent and temporary sleepers must both respect occupants and booked arrivals.
        IntVec3 sleepingPosition = RestUtility.GetBedSleepingSlotPosFor(sleeper, bed);
        __result = comp.IsSleepingPlaceAvailable(sleeper, sleepingPosition);
    }

    [Postfix]
    [Inner(typeof(Building_Bed), nameof(Building_Bed.IsOwner), typeof(Pawn), typeof(Out<int?>))]
    [Target(nameof(RestUtility.CanUseBedNow))]
    public static void IsOwner_Postfix(
        [Instance] Building_Bed bed,
        [Argument("sleeper", Scope.Outer)] Pawn sleeper,
        ref int? assignedSleepingSlot,
        ref bool __result)
    {
        if (!BedSharingHelpers.CanSleepInSharedBed(sleeper, bed))
            return;
        if (bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(sleeper, out int slot) != true)
            return;

        __result = true;
        assignedSleepingSlot = slot;
    }

    [Postfix]
    [Inner(typeof(CompAssignableToPawn), nameof(CompAssignableToPawn.IdeoligionForbids))]
    [Target(nameof(RestUtility.CanUseBedNow))]
    public static void IdeoligionForbids_Postfix(
        [Argument("sleeper", Scope.Outer)] Pawn sleeper,
        [Argument("bedThing", Scope.Outer)] Thing bedThing,
        ref bool __result)
    {
        // A temporary assignment concerns the bedmates present, rather than absent permanent owners.
        if (bedThing is Building_Bed bed && BedSharingHelpers.CanUseTemporarySleepingSlot(sleeper, bed))
            __result = false;
    }

    [Prefix]
    [Target(nameof(RestUtility.GetBedSleepingSlotPosFor))]
    public static bool GetBedSleepingSlotPosFor_Prefix(Pawn pawn, Building_Bed bed, out IntVec3 __result)
    {
        __result = default;
        if (bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(pawn, out int slot) != true)
            return true;

        // Running the original method and then changing the result might log a “Could not find good sleeping slot position” warning,
        // so this patch has to be a prefix.
        __result = bed.GetSleepingSlotPos(slot);
        return false;
    }
}
