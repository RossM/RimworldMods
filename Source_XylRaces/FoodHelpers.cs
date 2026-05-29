using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class FoodHelpers
{
    [Flags]
    public enum FoodType
    {
        None = 0x0,
        Meat = 0x1,
        NonMeat = 0x2,
        AnimalProduct = 0x4,
        Fungus = 0x8,
        Humanlike = 0x10,
        Insect = 0x20,
    }

    public static FoodType GetFoodType(ThingDef foodDef)
    {
        FoodTypeFlags flags = foodDef.ingestible?.foodType ?? 0;

        if (flags == FoodTypeFlags.Fungus)
            return FoodType.Fungus | FoodType.NonMeat;
        if ((flags & FoodTypeFlags.AnimalProduct) != 0)
            return FoodType.AnimalProduct;
        if ((flags & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant | FoodTypeFlags.Seed)) != 0)
            return FoodType.NonMeat;
        if ((flags & (FoodTypeFlags.Meat | FoodTypeFlags.Corpse)) != 0)
        {
            var foodType = FoodType.Meat;
            if (foodDef.ingestible?.sourceDef?.race?.Humanlike == true)
                foodType |= FoodType.Humanlike;
            if (foodDef.ingestible?.sourceDef?.race?.FleshType == FleshTypeDefOf.Insectoid)
                foodType |= FoodType.Insect;
            return foodType;
        }

        return FoodType.None;
    }

    public static float GetExtraNutritionFactor(Pawn eater, Thing foodSource, ThingDef foodDef)
    {
        if (foodDef.IsRawFoodOrCorpse)
        {
            return GetRawNutritionFactor(eater, GetFoodType(foodDef));
        }

        var compIngredients = foodSource.TryGetComp<CompIngredients>();
        if (compIngredients == null)
        {
            return GetCookedNutritionFactor(eater, GetFoodType(foodDef));
        }

        List<float> multipliers = [];
        foreach (var ingredient in compIngredients.ingredients)
        {
            multipliers.Add(GetCookedNutritionFactor(eater, GetFoodType(ingredient)));
        }

        return multipliers.Count > 0 ? (multipliers.Min() + multipliers.Max()) / 2 : 1.0f;
    }

    private static float GetRawNutritionFactor(Pawn eater, FoodType foodType)
    {
        float result = 1f;
        if (foodType.HasFlag(FoodType.Meat))
            result *= eater.GetStatValue(DefOf.XylRawMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.NonMeat))
            result *= eater.GetStatValue(DefOf.XylRawNonMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(DefOf.XylRawAnimalProductNutritionFactor);
        if (foodType.HasFlag(FoodType.Fungus))
            result *= eater.GetStatValue(DefOf.XylRawFungusNutritionFactor);
        return result;
    }

    private static float GetCookedNutritionFactor(Pawn eater, FoodType foodType)
    {
        float result = 1f;
        if (foodType.HasFlag(FoodType.Meat))
            result *= eater.GetStatValue(DefOf.XylCookedMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.NonMeat))
            result *= eater.GetStatValue(DefOf.XylCookedNonMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(DefOf.XylCookedAnimalProductNutritionFactor);
        //if (foodType.HasFlag(FoodType.Fungus))
        //    result *= eater.GetStatValue(DefOf.XylCookedFungusNutritionFactor);
        return result;
    }

    public static float GetFoodPoisonChanceFactor(Pawn eater, Thing foodSource)
    {
        var foodDef = foodSource.def;

        if (!foodDef.IsRawFoodOrCorpse)
            return 1f;

        FoodType foodType = GetFoodType(foodSource.def);
        float result = 1f;
        if (foodType.HasFlag(FoodType.Meat))
            result *= eater.GetStatValue(DefOf.XylRawMeatFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.NonMeat))
            result *= eater.GetStatValue(DefOf.XylRawNonMeatFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(DefOf.XylRawAnimalProductFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.Fungus))
            result *= eater.GetStatValue(DefOf.XylRawFungusFoodPoisonChanceFactor);
        return result;
    }

    public static bool IsThoughtFromIngestionDisallowedByGenes(
        Pawn eater,
        ThoughtDef thought,
        ThingDef ingestible,
        MeatSourceCategory meatSourceCategory)
    {
        if (thought == null || ingestible == null)
        {
            return false;
        }

        List<GeneIngestionThoughtOverride> thoughtOverrides = eater.GeneSet?.ingestionThoughtOverrides;
        if (thoughtOverrides == null)
            return false;

        foreach (var thoughtOverride in thoughtOverrides)
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

        return false;
    }

    public static float FoodOptimalityBonus(Pawn eater, Thing foodSource)
    {
        // Check if this food satisfies a diet dependency
        float extra = 0f;
        foreach (var hediff in eater.HediffsOfType<Hediff_DietDependency>())
        {
            if (hediff.ValidateFood(foodSource) && hediff.ShouldSatisfy)
                extra += 100f;
        }

        return extra;
    }
}
