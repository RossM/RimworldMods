using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore;

public static class FoodHelpers
{
    [DefOf]
    private static class Defs
    {
        [UsedImplicitly]
        public static StatDef XylRawFungusNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylRawMeatNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylRawAnimalProductNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylRawNonMeatNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylCookedMeatNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylCookedAnimalProductNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylCookedNonMeatNutritionFactor;
        [UsedImplicitly]
        public static StatDef XylRawFungusFoodPoisonChanceOffset;
        [UsedImplicitly]
        public static StatDef XylRawMeatFoodPoisonChanceOffset;
        [UsedImplicitly]
        public static StatDef XylRawAnimalProductFoodPoisonChanceOffset;
        [UsedImplicitly]
        public static StatDef XylRawNonMeatFoodPoisonChanceOffset;
    }

    public enum FoodType
    {
        None,
        Meat,
        NonMeat,
        Fungus,
        AnimalProduct,
    }

    public static FoodType GetFoodType(ThingDef foodDef)
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
        if (!foodDef.ingestible.foodType.HasFlag(FoodTypeFlags.Meal))
        {
            return GetFoodType(foodDef) switch
            {
                FoodType.Fungus => eater.GetStatValue(Defs.XylRawFungusNutritionFactor) *
                                   eater.GetStatValue(Defs.XylRawNonMeatNutritionFactor),
                FoodType.Meat => eater.GetStatValue(Defs.XylRawMeatNutritionFactor),
                FoodType.AnimalProduct => eater.GetStatValue(Defs.XylRawAnimalProductNutritionFactor),
                FoodType.NonMeat => eater.GetStatValue(Defs.XylRawNonMeatNutritionFactor),
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
                switch (GetFoodType(ingredient))
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
                multiplier += eater.GetStatValue(Defs.XylCookedMeatNutritionFactor);
                divisor += 1.0f;
            }

            if (hasAnimalProduct)
            {
                multiplier += eater.GetStatValue(Defs.XylCookedAnimalProductNutritionFactor);
                divisor += 1.0f;
            }

            if (hasNonMeat)
            {
                multiplier += eater.GetStatValue(Defs.XylCookedNonMeatNutritionFactor);
                divisor += 1.0f;
            }

            return divisor > 0 ? multiplier / divisor : 1.0f;

        }
    }

    public static float GetFoodPoisonChanceOffset(Pawn eater, Thing foodSource)
    {
        var foodDef = foodSource.def;

        if (!foodDef.ingestible.foodType.HasFlag(FoodTypeFlags.Meal)) 
            return 0.0f;

        return GetFoodType(foodSource.def) switch
        {
            FoodType.Fungus => eater.GetStatValue(Defs.XylRawFungusFoodPoisonChanceOffset) +
                               eater.GetStatValue(Defs.XylRawNonMeatFoodPoisonChanceOffset),
            FoodType.Meat => eater.GetStatValue(Defs.XylRawMeatFoodPoisonChanceOffset),
            FoodType.AnimalProduct => eater.GetStatValue(Defs.XylRawAnimalProductFoodPoisonChanceOffset),
            FoodType.NonMeat => eater.GetStatValue(Defs.XylRawNonMeatFoodPoisonChanceOffset),
            _ => 0.0f
        };
    }

    public static bool IsThoughtFromIngestionDisallowedByGenes(Pawn eater, ThoughtDef thought, ThingDef ingestible,
        MeatSourceCategory meatSourceCategory)
    {
        if (thought == null || ingestible == null)
        {
            return false;
        }

        foreach (var ext in eater.GeneDefExtensionsOfType<Genes.GeneDefExtension_IngestionThoughtOverride>())
        {
            foreach (var thoughtOverride in ext.thoughtOverrides.EmptyIfNull())
            {
                if (thoughtOverride.thoughts.NullOrEmpty())
                    continue;

                if (thoughtOverride.thing != null && thoughtOverride.thing != ingestible)
                    continue;

                if (!thoughtOverride.meatSources.NullOrEmpty() && !thoughtOverride.meatSources.Contains(meatSourceCategory))
                    continue;

                if (thoughtOverride.thoughts.Any(t => t == thought))
                {
                    return true;
                }
            }
        }

        return false;
    }
}