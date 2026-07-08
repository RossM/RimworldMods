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
    Any = Meat | Vegetable | AnimalProduct | Fungus | Humanlike | Insect
}

public static class FoodHelpers
{
    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;
    }

    extension(ThingDef foodDef)
    {
        public FoodType FoodType
        {
            get
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

                    RaceProperties? race = foodDef.ingestible?.sourceDef?.race;
                    if (race?.Humanlike is true)
                        foodType |= FoodType.Humanlike;
                    if (race?.FleshType == FleshTypeDefOf.Insectoid)
                        foodType |= FoodType.Insect;

                    return foodType;
                }

                return FoodType.None;
            }
        }
    }

    extension(Pawn eater)
    {
        public float GetExtraNutritionFactor(Thing foodSource, ThingDef foodDef)
        {
            if (foodDef.IsRawFoodOrCorpse)
            {
                return eater.GetRawNutritionFactor(foodDef.FoodType);
            }

            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients is not { ingredients: not null })
            {
                return eater.GetCookedNutritionFactor(foodDef.FoodType);
            }

            List<float> multipliers = [];
            foreach (var ingredient in compIngredients.ingredients)
            {
                multipliers.Add(eater.GetCookedNutritionFactor(ingredient.FoodType));
            }

            return multipliers.Count > 0 ? (multipliers.Min() + multipliers.Max()) / 2 : 1.0f;
        }

        private float GetRawNutritionFactor(FoodType foodType)
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

        private float GetCookedNutritionFactor(FoodType foodType)
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

        public float GetFoodPoisonChanceFactor(Thing foodSource)
        {
            var foodDef = foodSource.def;

            if (!foodDef.IsRawFoodOrCorpse)
                return 1f;

            FoodType foodType = foodSource.def.FoodType;
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
}
