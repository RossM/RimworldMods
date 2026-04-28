using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(HediffGiver_Hypothermia))]
    public static class Patch_HediffGiver_Hypothermia
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] 
            public static StatDef XylHypothermiaProgressionFactor;
        }

        private static readonly InstructionMatcher Fixup_OnIntervalPassed = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(HealthUtility), nameof(HealthUtility.AdjustSeverity)),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(typeof(Patch_HediffGiver_Hypothermia), nameof(StatDefOfHypothermiaProgressionFactor)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                        new CodeInstruction(OpCodes.Mul),
                    ]
                }
            }
        };

        public static StatDef StatDefOfHypothermiaProgressionFactor()
        {
            return Defs.XylHypothermiaProgressionFactor;
        }

        [Feature(nameof(Defs.XylHypothermiaProgressionFactor)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("OnIntervalPassed")]
        public static IEnumerable<CodeInstruction> OnIntervalPassed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_OnIntervalPassed.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

    }
}
