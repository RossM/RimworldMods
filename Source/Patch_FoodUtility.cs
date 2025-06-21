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
                {
                    StatDef rawFungusNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("RawFungusNutritionFactor", errorOnFail: false);
                    return rawFungusNutritionFactor != null ? eater.GetStatValue(rawFungusNutritionFactor) : 1.0f;
                }

                if (foodKind == FoodKind.Meat)
                {
                    StatDef rawMeatNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("RawMeatNutritionFactor", errorOnFail: false);
                    return rawMeatNutritionFactor != null ? eater.GetStatValue(rawMeatNutritionFactor) : 1.0f;
                }

                if (foodDef.IsAnimalProduct)
                {
                    StatDef rawAnimalProductNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("RawAnimalProductNutritionFactor", errorOnFail: false);
                    return rawAnimalProductNutritionFactor != null ? eater.GetStatValue(rawAnimalProductNutritionFactor) : 1.0f;
                }

                if (foodKind == FoodKind.NonMeat)
                {
                    StatDef rawNonMeatNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("RawNonMeatNutritionFactor", errorOnFail: false);
                    return rawNonMeatNutritionFactor != null ? eater.GetStatValue(rawNonMeatNutritionFactor) : 1.0f;
                }

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

                    StatDef cookedMeatNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("CookedMeatNutritionFactor", errorOnFail: false);
                    if (cookedMeatNutritionFactor != null && hasMeat)
                    {
                        multiplier += eater.GetStatValue(cookedMeatNutritionFactor);
                        divisor += 1.0f;
                    }

                    StatDef cookedAnimalProductNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("CookedAnimalProductNutritionFactor", errorOnFail: false);
                    if (cookedAnimalProductNutritionFactor != null && hasAnimalProduct)
                    {
                        multiplier += eater.GetStatValue(cookedAnimalProductNutritionFactor);
                        divisor += 1.0f;
                    }

                    StatDef cookedNonMeatNutritionFactor =
                        DefDatabase<StatDef>.GetNamed("CookedNonMeatNutritionFactor", errorOnFail: false);
                    if (cookedNonMeatNutritionFactor != null && hasNonMeat)
                    {
                        multiplier += eater.GetStatValue(cookedNonMeatNutritionFactor);
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
                    if (gene.ValidateFood(foodSource) && gene.LinkedHediff.Severity >= 0.5f)
                        __result += 100f;
                }
            }
        }
    }
}
