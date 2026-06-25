using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xylib.Patches
{
    [HarmonyPatch(typeof(FoodUtility))]
    public static class Patch_FoodUtility
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
            float nutritionFactor = FoodHelpers.GetExtraNutritionFactor(eater, foodSource, foodDef);

            // Adjust based on nutrition
            __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes *
                        ((nutritionFactor - 1.0f) / 0.8f);
        }

        [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
        [HarmonyPostfix]
        [HarmonyPatch("NutritionForEater")]
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
}
