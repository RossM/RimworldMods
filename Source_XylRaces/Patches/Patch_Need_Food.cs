using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Need_Food))]
    public static class Patch_Need_Food
    {
        public static Lazy<bool> enabled = new(() => Config.FeatureEnabled(Config.Feature.FixLactationBugs));
        public static bool Enabled => enabled.Value;

        [DefOf]
        public static class Defs
        {
            [UsedImplicitly]
            public static StatDef XylMalnutritionProgressionFactor;
        }

        private static readonly InstructionMatcher Fixup_FoodFallPerTickAssumingCategory = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(AccessTools.Method(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay)),
                    AccessTools.Method(typeof(Patch_Need_Food), nameof(AddedNutritionPerDay_Wrapper)))
            }
        };

        [Feature(nameof(Config.Feature.FixLactationBugs)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_FoodFallPerTickAssumingCategory.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        static float AddedNutritionPerDay_Wrapper(HediffComp_Lactating __instance)
        {
            if (Enabled)
                return 0;

            return __instance.AddedNutritionPerDay();
        }
    }
}
