using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.NutritionForEater))]
    public class Patch_FoodUtility_NutritionForEater
    {
        [HarmonyPostfix]
        static void Postfix(Pawn eater, Thing food, ref float __result)
        {
            StatDef rawFungusNutritionFactor =
                DefDatabase<StatDef>.GetNamed("RawFungusNutritionFactor", errorOnFail: false);
            if (rawFungusNutritionFactor != null && (food.def.ingestible.foodType & FoodTypeFlags.Fungus) == FoodTypeFlags.Fungus)
            {
                __result *= eater.GetStatValue(rawFungusNutritionFactor);
            }
        }
    }
}
