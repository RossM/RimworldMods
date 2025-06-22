using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(FoodUtility))]
    public class Patch_FoodUtility_NutritionForEater
    {
        [HarmonyPostfix, HarmonyPatch(nameof(FoodUtility.NutritionForEater))]
        static void NutritionForEater_Postfix(Pawn eater, Thing food, ref float __result)
        {
            __result *= GetNutritionFactor(eater, food, food.def);
        }

        private static float GetNutritionFactor(Pawn eater, Thing foodSource, ThingDef foodDef)
        {
            if (!foodDef.IsProcessedFood)
            {
                var foodKind = FoodUtility.GetFoodKind(foodSource);

                if (foodDef.IsFungus)
                    return eater.GetStatValue("RawFungusNutritionFactor");

                if (foodKind == FoodKind.Meat)
                    return eater.GetStatValue("RawMeatNutritionFactor");

                if (foodDef.IsAnimalProduct)
                    return eater.GetStatValue("RawAnimalProductNutritionFactor");

                if (foodKind == FoodKind.NonMeat)
                    return eater.GetStatValue("RawNonMeatNutritionFactor");

                return 1.0f;
            }
            else
            {
                var compIngredients = foodSource.TryGetComp<CompIngredients>();
                if (compIngredients != null)
                {
                    bool hasMeat = false;
                    bool hasAnimalProduct = false;
                    bool hasNonMeat = false;

                    foreach (var ingredient in compIngredients.ingredients)
                    {
                        var foodKind = FoodUtility.GetFoodKind(ingredient);

                        if (foodKind == FoodKind.Meat)
                            hasMeat = true;
                        if (foodKind == FoodKind.NonMeat)
                            hasNonMeat = true;
                        if (ingredient.IsAnimalProduct)
                            hasAnimalProduct = true;
                    }

                    float multiplier = 0.0f;
                    float divisor = 0.0f;

                    if (hasMeat)
                    {
                        multiplier += eater.GetStatValue("CookedMeatNutritionFactor");
                        divisor += 1.0f;
                    }

                    if (hasAnimalProduct)
                    {
                        multiplier += eater.GetStatValue("CookedAnimalProductNutritionFactor");
                        divisor += 1.0f;
                    }

                    if (hasNonMeat)
                    {
                        multiplier += eater.GetStatValue("CookedNonMeatNutritionFactor");
                        divisor += 1.0f;
                    }

                    if (divisor > 0)
                    {
                        return multiplier / divisor;
                    }
                    else
                    {
                        return 1.0f;
                    }
                }

                return 1.0f;
            }
        }

        [HarmonyPostfix, HarmonyPatch(nameof(FoodUtility.FoodOptimality))]
        static void FoodOptimality_Postfix(Pawn eater, Thing foodSource, ThingDef foodDef, float dist,
            bool takingToInventory, ref float __result)
        {
            float nutritionFactor = GetNutritionFactor(eater, foodSource, foodDef);

            // Adjust based on nutrition
            __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes * ((nutritionFactor - 1.0f) / 0.8f);

            // Check if this food satisfies a diet dependency
            if (eater.genes != null)
            {
                foreach (var gene in eater.genes.GenesListForReading.OfType<Gene_DietDependency>())
                {
                    if (gene.ValidateFood(foodSource) && ((Hediff_DietDependency)gene.LinkedHediff).ShouldSatisfy)
                        __result += 100f;
                }
            }
        }
    }
}
