using static RimWorld.FoodUtility;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(FoodUtility))]
public static class Patch_FoodUtility
{
    [Feature(typeof(Hediff_DietDependency))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FoodOptimality))]
    public static void FoodOptimality_Postfix(
        Pawn eater,
        Thing foodSource,
        ref float __result)
    {
        __result += PatchHelpers.FoodOptimalityBonus(eater, foodSource);
    }
}
