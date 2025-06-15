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

        }
    }
}
