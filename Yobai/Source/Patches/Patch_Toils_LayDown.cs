namespace Yobai.Patches;

[Patch(typeof(Toils_LayDown))]
internal static class Patch_Toils_LayDown
{
    [Postfix]
    [Inner(typeof(Pawn_Ownership), nameof(Pawn_Ownership.OwnedBed), MemberType.Getter)]
    [Target("ApplyBedThoughts")]
    public static void OwnedBed_Postfix(Pawn actor, Building_Bed? bed, ref Building_Bed? __result)
    {
        if (bed?.GetComp<CompBedSharing>()?.HasSleepingSlot(actor) == true)
            __result = bed;
    }
}
