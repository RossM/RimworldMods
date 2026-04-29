using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Need_Food))]
    public static class Patch_Need_Food
    {
        public static Lazy<bool> Enabled = new(() => Config.FeatureEnabled(Config.Feature.FixLactationBugs));

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
                InstructionMatcher.RedirectMethodRule(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay),
                    typeof(Patch_Need_Food), nameof(AddedNutritionPerDay))
            }
        };

        [Feature(nameof(Config.Feature.FixLactationBugs)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_FoodFallPerTickAssumingCategory.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        static float AddedNutritionPerDay(HediffComp_Lactating hediffComp)
        {
            if (Enabled.Value)
                return 0;

            return hediffComp.AddedNutritionPerDay();
        }
    }
}
