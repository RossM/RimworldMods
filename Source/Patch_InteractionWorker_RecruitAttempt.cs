using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
    static class Patch_InteractionWorker_RecruitAttempt
    {
        static StatDef StatDefOfResistanceFallRate()
        {
            return DefDatabase<StatDef>.GetNamed("ResistanceFallRate");
        }

        private static readonly InstructionMatcher ApplyResistanceFallRate = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Pattern =
                    [
                        // statValue2 = initiator.GetStatValue(StatDefOf.NegotiationAbility);
                        CodeInstruction.LoadArgument(1),
                        new CodeInstruction(OpCodes.Ldsfld, typeof(StatDefOf).GetField(nameof(StatDefOf.NegotiationAbility))),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue)), 
                    ],
                    Output =
                    [
                        // statValue2 *= recipient.GetStatValue(StatDefOfResistanceFallRate());
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(typeof(Patch_InteractionWorker_RecruitAttempt), nameof(Patch_InteractionWorker_RecruitAttempt.StatDefOfResistanceFallRate)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                        new CodeInstruction(OpCodes.Mul),
                    ]
                }
            }
        };

        [HarmonyTranspiler, HarmonyPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
        static IEnumerable<CodeInstruction> Interacted_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!ApplyResistanceFallRate.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetOffsetsAndFactorsExplanation_Transpiler: {0}", reason));
            return instructionsList;
        }
    }
}
