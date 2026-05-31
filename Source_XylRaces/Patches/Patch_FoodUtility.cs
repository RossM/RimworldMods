using static RimWorld.FoodUtility;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(FoodUtility))]
public static class Patch_FoodUtility_NutritionForEater
{
    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FoodOptimality))]
    public static void FoodOptimality_Postfix(
        Pawn eater,
        Thing foodSource,
        ThingDef foodDef,
        ref float __result)
    {
        float nutritionFactor = FoodHelpers.GetExtraNutritionFactor(eater, foodSource, foodDef);

        // Adjust based on nutrition
        __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes *
                    ((nutritionFactor - 1.0f) / 0.8f);

        __result += FoodHelpers.FoodOptimalityBonus(eater, foodSource);
    }

    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutritionForEater))]
    public static void NutritionForEater_Postfix(Pawn eater, Thing food, ref float __result)
    {
        __result *= FoodHelpers.GetExtraNutritionFactor(eater, food, food.def);
    }

    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [HarmonyPrefix]
    [HarmonyPatch("TryAddIngestThought")]
    public static bool TryAddIngestThought_Prefix(
        Pawn ingester,
        ThoughtDef def,
        ThingDef foodDef,
        MeatSourceCategory meatSourceCategory)
    {
        return !FoodHelpers.IsThoughtFromIngestionDisallowedByGenes(ingester, def, foodDef, meatSourceCategory);
    }
}
