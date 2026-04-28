using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.Call(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay)),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadField(typeof(Need), "pawn"),
                        CodeInstruction.Call(() => GetAddedNutritionPerDay),
                    ]
                }
            }
        };

        [Feature(nameof(Config.Feature.FixLactationBugs)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_FoodFallPerTickAssumingCategory.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        static float GetAddedNutritionPerDay(Pawn pawn)
        {
            if (Enabled.Value)
                return 0;

            using (new ProfileBlock())
            {
                return PatchLactation.GetFirstLactationHediff(pawn.health.hediffSet)?.TryGetComp<HediffComp_Lactating>()
                    ?.AddedNutritionPerDay() ?? 0;
            }
        }

        [Feature(nameof(Defs.XylMalnutritionProgressionFactor)), HarmonyPostfix,
         HarmonyPatch("MalnutritionSeverityPerInterval", MethodType.Getter)]
        public static void MalnutritionSeverityPerInterval_Postfix(Need_Food __instance, ref float __result)
        {
            __result *= __instance.pawn.GetStatValue(Defs.XylMalnutritionProgressionFactor);
        }
    }
}
