namespace Xylib.Patches;

[HarmonyPatch(typeof(JoyGiver))]
public class Patch_JoyGiver
{
    [Feature(nameof(DefModExtension_GeneWithComps.joyGiverChanceFactors))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(JoyGiver.GetChance))]
    public static void GetChance_Postfix(JoyGiver __instance, Pawn pawn, ref float __result)
    {
        var factor = PatchHelpers.GetJoyFactor(pawn, __instance);
        __result *= factor;
    }
}
