using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay))]
        [InfixPatch("FoodFallPerTickAssumingCategory")]
        static float AddedNutritionPerDay_Wrapper(HediffComp_Lactating __instance)
        {
            if (Settings.instance.ShouldFixLactationBugsFor(__instance.Pawn))
                return 0;

            return __instance.AddedNutritionPerDay();
        }
    }
}
