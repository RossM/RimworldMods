using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Need_Food))]
    public static class Patch_Need_Food
    {
        private static readonly InstructionMatcher Fixup_FoodFallPerTickAssumingCategory = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(HediffComp_Lactating.AddedNutritionPerDay), AddedNutritionPerDay_Wrapper)
            }
        };

        [Feature(Config.Feature.FixLactationBugs)]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch("FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_FoodFallPerTickAssumingCategory.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float AddedNutritionPerDay_Wrapper(HediffComp_Lactating __instance)
        {
            if (Settings.instance.ShouldFixLactationBugsFor(__instance.Pawn))
                return 0;

            return __instance.AddedNutritionPerDay();
        }
    }
}
