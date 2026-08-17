namespace XylIdeos;

[HarmonyPatch(typeof(ApparelUtility))]
public static class Patch_ApparelUtility
{
    [Feature(Features.NudityIsGenderSpecific)]
    [Postfix] [Inner(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudity))]
    [Target(nameof(ApparelUtility.IsRequirementActive))]
    public static void IdeoPrefersNudity_Postfix(Ideo ideo, Pawn pawn, ref bool __result)
    {
        __result = ideo.IdeoPrefersNudityForGender(pawn.gender);
    }
}
