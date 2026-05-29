using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Need_Food))]
    public static class Patch_Need_Food
    {
        [Feature(Config.Feature.FixLactationBugs)]
        [InfixPostfix(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay))]
        [InfixPatch("FoodFallPerTickAssumingCategory")]
        private static void AddedNutritionPerDay_Postfix(HediffComp_Lactating __instance, ref float __result)
        {
            if (Settings.instance.ShouldFixLactationBugsFor(__instance.Pawn))
                __result = 0f;
        }
    }
}
