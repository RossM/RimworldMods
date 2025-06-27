using RimWorld;
using Verse;

namespace XylRacesCore;

public static class FoodHelpers
{
    public enum FoodType
    {
        None,
        Meat,
        NonMeat,
        Fungus,
        AnimalProduct,
    }

    public static FoodType GetBestFoodType(ThingDef foodDef)
    {
        FoodTypeFlags flags = foodDef.ingestible?.foodType ?? 0;

        if (flags == FoodTypeFlags.Fungus)
            return FoodType.Fungus;
        if ((flags & FoodTypeFlags.AnimalProduct) != 0)
            return FoodType.AnimalProduct;
        if ((flags & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant | FoodTypeFlags.Seed)) != 0)
            return FoodType.NonMeat;
        if ((flags & (FoodTypeFlags.Meat | FoodTypeFlags.Corpse)) != 0)
            return FoodType.Meat;
        return FoodType.None;
    }

    public static float GetExtraNutritionFactor(Pawn eater, Thing foodSource, ThingDef foodDef)
    {
        if (!foodDef.IsProcessedFood)
        {
            return GetBestFoodType(foodDef) switch
            {
                FoodType.Fungus => eater.GetStatValue("RawFungusNutritionFactor", 1.0f) *
                                   eater.GetStatValue("RawNonMeatNutritionFactor", 1.0f),
                FoodType.Meat => eater.GetStatValue("RawMeatNutritionFactor", 1.0f),
                FoodType.AnimalProduct => eater.GetStatValue("RawAnimalProductNutritionFactor", 1.0f),
                FoodType.NonMeat => eater.GetStatValue("RawNonMeatNutritionFactor", 1.0f),
                _ => 1.0f
            };
        }
        else
        {
            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients == null) 
                return 1.0f;

            var hasMeat = false;
            var hasAnimalProduct = false;
            var hasNonMeat = false;

            foreach (var ingredient in compIngredients.ingredients)
            {
                switch (GetBestFoodType(ingredient))
                {
                    case FoodType.Meat:
                        hasMeat = true;
                        break;
                    case FoodType.NonMeat:
                    case FoodType.Fungus:
                        hasNonMeat = true;
                        break;
                    case FoodType.AnimalProduct:
                        hasAnimalProduct = true;
                        break;
                }
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
    }

    public static float GetFoodPoisoningChanceOffset(Pawn eater, Thing foodSource)
    {
        var foodDef = foodSource.def;

        if (foodDef.IsProcessedFood) 
            return 0.0f;

        return GetBestFoodType(foodSource.def) switch
        {
            FoodType.Fungus => eater.GetStatValue("RawFungusFoodPoisoningChanceOffset", 0.0f) +
                               eater.GetStatValue("RawNonMeatFoodPoisoningChanceOffset", 0.0f),
            FoodType.Meat => eater.GetStatValue("RawMeatFoodPoisoningChanceOffset", 0.0f),
            FoodType.AnimalProduct => eater.GetStatValue("RawAnimalProductFoodPoisoningChanceOffset", 0.0f),
            FoodType.NonMeat => eater.GetStatValue("RawNonMeatFoodPoisoningChanceOffset", 0.0f),
            _ => 0.0f
        };
    }
}