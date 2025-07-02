using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(FoodUtility))]
    public static class Patch_FoodUtility_NutritionForEater
    {
        [HarmonyPostfix, HarmonyPatch(nameof(FoodUtility.NutritionForEater))]
        static void NutritionForEater_Postfix(Pawn eater, Thing food, ref float __result)
        {
            __result *= FoodHelpers.GetExtraNutritionFactor(eater, food, food.def);
        }

        [HarmonyPostfix, HarmonyPatch(nameof(FoodUtility.FoodOptimality))]
        static void FoodOptimality_Postfix(Pawn eater, Thing foodSource, ThingDef foodDef, float dist,
            bool takingToInventory, ref float __result)
        {
            float nutritionFactor = FoodHelpers.GetExtraNutritionFactor(eater, foodSource, foodDef);

            // Adjust based on nutrition
            __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes * ((nutritionFactor - 1.0f) / 0.8f);

            // Check if this food satisfies a diet dependency
            foreach (var gene in eater.GenesOfType<Gene_DietDependency>())
            {
                if (gene.ValidateFood(foodSource) && ((Hediff_DietDependency)gene.LinkedHediff).ShouldSatisfy)
                    __result += 100f;
            }
        }
    }
}
