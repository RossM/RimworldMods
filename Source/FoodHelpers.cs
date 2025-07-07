using JetBrains.Annotations;
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

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
        using (new ProfileBlock())
        {
            if (IsRawFoodOrCorpse(foodDef))
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

            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients == null)
            {
                return GetFoodType(foodDef) switch
                {
                    FoodType.Meat => eater.GetStatValue(Defs.XylCookedMeatNutritionFactor),
                    FoodType.AnimalProduct => eater.GetStatValue(Defs.XylCookedAnimalProductNutritionFactor),
                    FoodType.Fungus or FoodType.NonMeat => eater.GetStatValue(Defs.XylCookedNonMeatNutritionFactor),
                    _ => 1.0f
                };
            }

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
        using (new ProfileBlock())
        {
            var foodDef = foodSource.def;

            if (!IsRawFoodOrCorpse(foodDef))
                return 0.0f;

            FoodType foodType = GetFoodType(foodSource.def);
            var value = foodType switch
            {
                FoodType.Fungus => eater.GetStatValue(Defs.XylRawFungusFoodPoisonChanceOffset) +
                                   eater.GetStatValue(Defs.XylRawNonMeatFoodPoisonChanceOffset),
                FoodType.Meat => eater.GetStatValue(Defs.XylRawMeatFoodPoisonChanceOffset),
                FoodType.AnimalProduct => eater.GetStatValue(Defs.XylRawAnimalProductFoodPoisonChanceOffset),
                FoodType.NonMeat => eater.GetStatValue(Defs.XylRawNonMeatFoodPoisonChanceOffset),
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

            foreach (var ext in eater.GeneDefExtensionsOfType<Genes.GeneDefExtension_IngestionThoughtOverride>())
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