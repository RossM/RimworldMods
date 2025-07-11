using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(ITab_Pawn_Visitor))]
    public static class Patch_ITab_Pawn_Visitor
    {
        private static readonly InstructionMatcher Fixup = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(StatWorker_SuppressionFallRate), nameof(StatWorker_SuppressionFallRate.GetExplanationForTooltip))),
                    ],
                    Output =
                    [
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(StatWorker_SuppressionFallRate_Fixed), nameof(StatWorker_SuppressionFallRate_Fixed.GetExplanationForTooltip))),
                    ]
                },
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Castclass, typeof(StatWorker_SuppressionFallRate)),
                    ],
                    Output =
                    [
                        new CodeInstruction(OpCodes.Castclass, typeof(StatWorker_SuppressionFallRate_Fixed)),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DoSlaveTab")]
        public static IEnumerable<CodeInstruction> DoSlaveTab_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }
    }
}
