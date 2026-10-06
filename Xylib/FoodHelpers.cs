namespace Xylib;

public static class FoodHelpers
{
    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;

        public IEnumerable<FoodGroupDef> FoodGroups
        {
            get
            {
                FoodTypeFlags flags = thingDef.ingestible?.foodType ?? 0;
                RaceProperties? race = thingDef.ingestible?.sourceDef?.race;

                foreach (var foodGroup in DefDatabase<FoodGroupDef>.AllDefs)
                {
                    if (foodGroup.exact)
                    {
                        if (foodGroup.foodTypes != flags)
                            continue;
                    }
                    else if (foodGroup.foodTypes != 0 && (foodGroup.foodTypes & flags) == 0)
                    {
                        continue;
                    }

                    if (foodGroup.humanlike && race?.Humanlike is not true)
                        continue;
                    if (foodGroup.fleshType != null && race?.FleshType != foodGroup.fleshType)
                        continue;

                    yield return foodGroup;
                }
            }
        }
    }

    extension(Pawn eater)
    {
        public float GetExtraNutritionFactor(Thing foodSource, ThingDef foodDef)
        {
            if (foodDef.IsRawFoodOrCorpse)
                return eater.GetRawNutritionFactor(foodDef.FoodGroups);

            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients is null)
                return eater.GetCookedNutritionFactor(foodDef.FoodGroups);

            List<float> multipliers = [];
            foreach (var ingredient in compIngredients.ingredients)
                multipliers.Add(eater.GetCookedNutritionFactor(ingredient.FoodGroups));

            return multipliers.Count > 0 ? (multipliers.Min() + multipliers.Max()) / 2 : 1.0f;
        }

        private float GetRawNutritionFactor(IEnumerable<FoodGroupDef> foodGroups)
        {
            float result = 1f;
            foreach (var food in foodGroups)
            {
                if (food.rawNutritionStat != null)
                    result *= eater.GetStatValue(food.rawNutritionStat);
            }

            return result;
        }

        private float GetCookedNutritionFactor(IEnumerable<FoodGroupDef> foodGroups)
        {
            float result = 1f;
            foreach (var food in foodGroups)
            {
                if (food.cookedNutritionStat != null)
                    result *= eater.GetStatValue(food.cookedNutritionStat);
            }

            return result;
        }

        public float GetFoodPoisonChanceFactor(Thing foodSource)
        {
            var foodDef = foodSource.def;

            if (!foodDef.IsRawFoodOrCorpse)
                return 1f;

            IEnumerable<FoodGroupDef> foodGroups = foodSource.def.FoodGroups;
            float result = 1f;
            foreach (var food in foodGroups)
            {
                if (food.rawFoodPoisonChanceStat != null)
                    result *= eater.GetStatValue(food.rawFoodPoisonChanceStat);
            }

            return result;
        }
    }
}
