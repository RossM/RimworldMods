namespace XylIdeos;

[HarmonyPatch(typeof(ApparelUtility))]
public class Patch_ApparelUtility
{
    [Feature(Features.NudityIsGenderSpecific)]
    [InfixPostfix(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudity))]
    [InfixPatch(nameof(ApparelUtility.IsRequirementActive))]
    public static void IdeoPrefersNudity_Postfix(Ideo ideo, Pawn pawn, ref bool __result)
    {
        __result = ideo.IdeoPrefersNudityForGender(pawn.gender);
    }
}
