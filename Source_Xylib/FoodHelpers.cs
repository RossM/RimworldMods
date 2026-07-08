namespace Xylib;

public static class FoodHelpers
{
    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;
    }

    extension(ThingDef foodDef)
    {
        public IEnumerable<FoodGroupDef> FoodGroups
        {
            get
            {
                FoodTypeFlags flags = foodDef.ingestible?.foodType ?? 0;
                RaceProperties? race = foodDef.ingestible?.sourceDef?.race;

                foreach (var food in DefDatabase<FoodGroupDef>.AllDefs)
                {
                    if (food.exact)
                    {
                        if (food.foodTypes != flags)
                            continue;
                    }
                    else if (food.foodTypes != 0 && (food.foodTypes & flags) == 0)
                        continue;

                    if (food.humanlike && race?.Humanlike is not true)
                        continue;
                    if (food.fleshType != null && race?.FleshType != food.fleshType)
                        continue;

                    yield return food;
                }
            }
        }
    }

    extension(Pawn eater)
    {
        public float GetExtraNutritionFactor(Thing foodSource, ThingDef foodDef)
        {
            if (foodDef.IsRawFoodOrCorpse)
            {
                return eater.GetRawNutritionFactor(foodDef.FoodGroups);
            }

            var compIngredients = foodSource.TryGetComp<CompIngredients>();
            if (compIngredients is not { ingredients: not null })
            {
                return eater.GetCookedNutritionFactor(foodDef.FoodGroups);
            }

            List<float> multipliers = [];
            foreach (var ingredient in compIngredients.ingredients)
            {
                multipliers.Add(eater.GetCookedNutritionFactor(ingredient.FoodGroups));
            }

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
