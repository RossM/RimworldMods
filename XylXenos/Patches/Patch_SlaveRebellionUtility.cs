namespace XylXenos.Patches;

[Patch(typeof(SlaveRebellionUtility))]
public static class Patch_SlaveRebellionUtility
{
    [Feature(nameof(DefOf.XylDocile))]
    [Postfix]
    [Target(nameof(SlaveRebellionUtility.CanParticipateInSlaveRebellion))]
    public static void CanParticipateInSlaveRebellion_Postfix(Pawn pawn, ref bool __result)
    {
        if (PatchHelpers.DocileAndHappy(pawn))
            __result = false;
    }
}
