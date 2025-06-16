using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.NutritionForEater))]
    public class Patch_FoodUtility_NutritionForEater
    {
        [HarmonyPostfix]
        static void Postfix(Pawn eater, Thing food, ref float __result)
        {
            if (!food.def.IsProcessedFood)
            {
                var foodKind = FoodUtility.GetFoodKind(food);

                StatDef rawFungusNutritionFactor =
                    DefDatabase<StatDef>.GetNamed("RawFungusNutritionFactor", errorOnFail: false);
                if (rawFungusNutritionFactor != null && food.def.IsFungus)
                {
                    __result *= eater.GetStatValue(rawFungusNutritionFactor);
                }

                StatDef rawMeatNutritionFactor =
                    DefDatabase<StatDef>.GetNamed("RawMeatNutritionFactor", errorOnFail: false);
                if (rawMeatNutritionFactor != null && foodKind == FoodKind.Meat)
                {
                    __result *= eater.GetStatValue(rawMeatNutritionFactor);
                }

                StatDef rawAnimalProductNutritionFactor =
                    DefDatabase<StatDef>.GetNamed("RawAnimalProductNutritionFactor", errorOnFail: false);
                if (rawAnimalProductNutritionFactor != null && food.def.IsAnimalProduct)
                {
                    __result *= eater.GetStatValue(rawAnimalProductNutritionFactor);
                }

                StatDef rawNonMeatNutritionFactor =
                    DefDatabase<StatDef>.GetNamed("RawNonMeatNutritionFactor", errorOnFail: false);
                if (rawNonMeatNutritionFactor != null && foodKind == FoodKind.NonMeat)
                {
                    __result *= eater.GetStatValue(rawNonMeatNutritionFactor);
                }
            }
            else
            {
                var compIngredients = food.TryGetComp<CompIngredients>();
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
                        __result *= multiplier / divisor;
                    }
                }
            }
        }
    }
}
