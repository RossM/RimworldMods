using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using Verse;
using XylRacesCore.Genes;
using static RimWorld.FoodUtility;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(FoodUtility))]
    public static class Patch_FoodUtility_NutritionForEater
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(NutritionForEater))]
        public static void NutritionForEater_Postfix(Pawn eater, Thing food, ref float __result)
        {
            using (new ProfileBlock())
            {
                __result *= FoodHelpers.GetExtraNutritionFactor(eater, food, food.def);
            }
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(FoodOptimality))]
        public static void FoodOptimality_Postfix(Pawn eater, Thing foodSource, ThingDef foodDef, float dist,
            bool takingToInventory, ref float __result)
        {
            using (new ProfileBlock())
            {
                float nutritionFactor = FoodHelpers.GetExtraNutritionFactor(eater, foodSource, foodDef);

                // Adjust based on nutrition
                __result += ThingDefOf.MealSimple.ingestible.optimalityOffsetHumanlikes *
                            ((nutritionFactor - 1.0f) / 0.8f);

                // Check if this food satisfies a diet dependency
                foreach (var gene in eater.GenesOfType<Gene_DietDependency>())
                {
                    if (gene.ValidateFood(foodSource) && ((Hediff_DietDependency)gene.LinkedHediff).ShouldSatisfy)
                        __result += 100f;
                }
            }
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("TryAddIngestThought")]
        public static bool TryAddIngestThought_Prefix(Pawn ingester, ThoughtDef def, Precept fromPrecept,
            List<ThoughtFromIngesting> ingestThoughts, ThingDef foodDef, MeatSourceCategory meatSourceCategory)
        {
            using (new ProfileBlock())
            {
                if (FoodHelpers.IsThoughtFromIngestionDisallowedByGenes(ingester, def, foodDef, meatSourceCategory))
                    return false;
                return true;
            }
        }
    }
}
