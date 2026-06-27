namespace Xylib;

[Flags]
public enum FoodType
{
    None = 0x0,
    Meat = 0x1,
    Vegetable = 0x2,
    AnimalProduct = 0x4,
    Fungus = 0x8,
    Humanlike = 0x10,
    Insect = 0x20,
}

public static class FoodHelpers
{
    public static FoodType GetFoodType(ThingDef foodDef)
    {
        FoodTypeFlags flags = foodDef.ingestible?.foodType ?? 0;

        if (flags == FoodTypeFlags.Fungus)
            return FoodType.Fungus | FoodType.Vegetable;
        if ((flags & FoodTypeFlags.AnimalProduct) != 0)
            return FoodType.AnimalProduct;
        if ((flags & (FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.Plant | FoodTypeFlags.Seed)) != 0)
            return FoodType.Vegetable;
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
            result *= eater.GetStatValue(XStatDefOf.XylRawMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.Vegetable))
            result *= eater.GetStatValue(XStatDefOf.XylRawVegetableNutritionFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(XStatDefOf.XylRawAnimalProductNutritionFactor);
        if (foodType.HasFlag(FoodType.Fungus))
            result *= eater.GetStatValue(XStatDefOf.XylRawFungusNutritionFactor);
        return result;
    }

    private static float GetCookedNutritionFactor(Pawn eater, FoodType foodType)
    {
        float result = 1f;
        if (foodType.HasFlag(FoodType.Meat))
            result *= eater.GetStatValue(XStatDefOf.XylCookedMeatNutritionFactor);
        if (foodType.HasFlag(FoodType.Vegetable))
            result *= eater.GetStatValue(XStatDefOf.XylCookedVegetableNutritionFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(XStatDefOf.XylCookedAnimalProductNutritionFactor);
        if (foodType.HasFlag(FoodType.Fungus))
            result *= eater.GetStatValue(XStatDefOf.XylCookedFungusNutritionFactor);
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
            result *= eater.GetStatValue(XStatDefOf.XylRawMeatFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.Vegetable))
            result *= eater.GetStatValue(XStatDefOf.XylRawVegetableFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.AnimalProduct))
            result *= eater.GetStatValue(XStatDefOf.XylRawAnimalProductFoodPoisonChanceFactor);
        if (foodType.HasFlag(FoodType.Fungus))
            result *= eater.GetStatValue(XStatDefOf.XylRawFungusFoodPoisonChanceFactor);
        return result;
    }
}
