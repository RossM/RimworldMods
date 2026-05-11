using System.Collections.Generic;
using System.Linq;
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
        using (new ProfileBlock())
        {
            if (IsRawFoodOrCorpse(foodDef))
            {
                return GetRawNutritionFactor(eater, GetFoodType(foodDef));
            }

            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients == null)
            {
                return GetCookedNutritionFactor(eater, GetFoodType(foodDef));
            }

            List<float> multipliers = new();
            foreach (var ingredient in compIngredients.ingredients)
            {
                multipliers.Add(GetCookedNutritionFactor(eater, GetFoodType(ingredient)));
            }

            return multipliers.Count > 0 ? (multipliers.Min() + multipliers.Max()) / 2 : 1.0f;
        }
    }

    private static float GetRawNutritionFactor(Pawn eater, FoodType foodType)
    {
        return foodType switch
        {
            FoodType.Fungus => eater.GetStatValue(DefOf.XylRawFungusNutritionFactor) *
                               eater.GetStatValue(DefOf.XylRawNonMeatNutritionFactor),
            FoodType.Meat => eater.GetStatValue(DefOf.XylRawMeatNutritionFactor),
            FoodType.AnimalProduct => eater.GetStatValue(DefOf.XylRawAnimalProductNutritionFactor),
            FoodType.NonMeat => eater.GetStatValue(DefOf.XylRawNonMeatNutritionFactor),
            _ => 1.0f
        };
    }

    private static float GetCookedNutritionFactor(Pawn eater, FoodType foodType)
    {
        return foodType switch
        {
            FoodType.Meat => eater.GetStatValue(DefOf.XylCookedMeatNutritionFactor),
            FoodType.AnimalProduct => eater.GetStatValue(DefOf.XylCookedAnimalProductNutritionFactor),
            FoodType.Fungus or FoodType.NonMeat => eater.GetStatValue(DefOf.XylCookedNonMeatNutritionFactor),
            _ => 1.0f
        };
    }

    public static float GetFoodPoisonChanceOffset(Pawn eater, Thing foodSource)
    {
        using (new ProfileBlock())
        {
            var foodDef = foodSource.def;

            if (!IsRawFoodOrCorpse(foodDef))
                return 0.0f;

            FoodType foodType = GetFoodType(foodSource.def);
            var value = foodType switch
            {
                FoodType.Fungus => eater.GetStatValue(DefOf.XylRawFungusFoodPoisonChanceOffset) +
                                   eater.GetStatValue(DefOf.XylRawNonMeatFoodPoisonChanceOffset),
                FoodType.Meat => eater.GetStatValue(DefOf.XylRawMeatFoodPoisonChanceOffset),
                FoodType.AnimalProduct => eater.GetStatValue(DefOf.XylRawAnimalProductFoodPoisonChanceOffset),
                FoodType.NonMeat => eater.GetStatValue(DefOf.XylRawNonMeatFoodPoisonChanceOffset),
                _ => 0.0f
            };
            return value;
        }
    }

    public static bool IsRawFoodOrCorpse(this ThingDef foodDef)
    {
        return (foodDef.IsRawHumanFood() || foodDef.IsCorpse);
    }

    public static bool IsThoughtFromIngestionDisallowedByGenes(Pawn eater, ThoughtDef thought, ThingDef ingestible,
        MeatSourceCategory meatSourceCategory)
    {
        using (new ProfileBlock())
        {
            if (thought == null || ingestible == null)
            {
                return false;
            }

            foreach (var ext in eater.ActiveGeneDefExtensionsOfType<Genes.GeneDefExtension_IngestionThoughtOverride>())
            {
                foreach (var thoughtOverride in ext.thoughtOverrides.EmptyIfNull())
                {
                    if (thoughtOverride.thoughts.NullOrEmpty())
                        continue;

                    if (thoughtOverride.thing != null && thoughtOverride.thing != ingestible)
                        continue;

                    if (!thoughtOverride.meatSources.NullOrEmpty() &&
                        !thoughtOverride.meatSources.Contains(meatSourceCategory))
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
}