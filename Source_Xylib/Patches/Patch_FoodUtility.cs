namespace Xylib.Patches;

[HarmonyPatch(typeof(FoodUtility))]
internal static class Patch_FoodUtility
{
    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [HarmonyPostfix]
    [HarmonyPatch("FoodOptimality")]
    public static void FoodOptimality_Postfix(
        Pawn eater,
        Thing foodSource,
        ThingDef foodDef,
        ref float __result)
    {
        float nutritionFactor = eater.GetExtraNutritionFactor(foodSource, foodDef);

        // Adjust based on nutrition
        __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes *
                    ((nutritionFactor - 1.0f) / 0.8f);
    }

    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [HarmonyPostfix]
    [HarmonyPatch("NutritionForEater")]
    public static void NutritionForEater_Postfix(Pawn eater, Thing food, ref float __result)
    {
        __result *= eater.GetExtraNutritionFactor(food, food.def);
    }
}
