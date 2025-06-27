using RimWorld;
using Verse;

namespace XylRacesCore;

public static class FoodHelpers
{
    public static float GetExtraNutritionFactor(Pawn eater, Thing foodSource, ThingDef foodDef)
    {
        if (!foodDef.IsProcessedFood)
        {
            var foodKind = FoodUtility.GetFoodKind(foodSource);

            if (foodDef.IsFungus)
                return eater.GetStatValue("RawFungusNutritionFactor", 1.0f) * 
                       eater.GetStatValue("RawNonMeatNutritionFactor", 1.0f);

            if (foodKind == FoodKind.Meat)
                return eater.GetStatValue("RawMeatNutritionFactor", 1.0f);

            if (foodDef.IsAnimalProduct)
                return eater.GetStatValue("RawAnimalProductNutritionFactor", 1.0f);

            if (foodKind == FoodKind.NonMeat)
                return eater.GetStatValue("RawNonMeatNutritionFactor", 1.0f);

            return 1.0f;
        }
        else
        {
            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients != null)
            {
                var hasMeat = false;
                var hasAnimalProduct = false;
                var hasNonMeat = false;

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

                var multiplier = 0.0f;
                var divisor = 0.0f;

                if (hasMeat)
                {
                    multiplier += eater.GetStatValue("CookedMeatNutritionFactor", 1.0f);
                    divisor += 1.0f;
                }

                if (hasAnimalProduct)
                {
                    multiplier += eater.GetStatValue("CookedAnimalProductNutritionFactor", 1.0f);
                    divisor += 1.0f;
                }

                if (hasNonMeat)
                {
                    multiplier += eater.GetStatValue("CookedNonMeatNutritionFactor", 1.0f);
                    divisor += 1.0f;
                }

                return divisor > 0 ? multiplier / divisor : 1.0f;
            }

            return 1.0f;
        }
    }
    public static float GetFoodPoisoningChanceOffset(Pawn eater, Thing foodSource)
    {
        var foodDef = foodSource.def;

        if (foodDef.IsProcessedFood) 
            return 0.0f;
        
        var foodKind = FoodUtility.GetFoodKind(foodSource);

        if (foodDef.IsFungus)
            return eater.GetStatValue("RawFungusFoodPoisoningChanceOffset", 0.0f) +
                   eater.GetStatValue("RawNonMeatFoodPoisoningChanceOffset", 0.0f);

        if (foodKind == FoodKind.Meat)
            return eater.GetStatValue("RawMeatFoodPoisoningChanceOffset", 0.0f);

        if (foodDef.IsAnimalProduct)
            return eater.GetStatValue("RawAnimalProductFoodPoisoningChanceOffset", 0.0f);

        if (foodKind == FoodKind.NonMeat)
            return eater.GetStatValue("RawNonMeatFoodPoisoningChanceOffset", 0.0f);

        return 0.0f;

    }
}