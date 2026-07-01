using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace Source_XylIdeoTweaks;

[HarmonyPatch(typeof(ApparelUtility))]
public class Patch_ApparelUtility
{
    [InfixPostfix(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudity))]
    [InfixPatch(nameof(ApparelUtility.IsRequirementActive))]
    public static void IdeoPrefersNudity_Postfix(Ideo ideo, Pawn pawn, ref bool __result)
    {
        __result = ideo.IdeoPrefersNudityForGender(pawn.gender);
    }
}
