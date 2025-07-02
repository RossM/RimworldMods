using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt))]
    public static class Patch_InteractionWorker_EnslaveAttempt
    {
        public static StatDef StatDefOfWillFallRate()
        {
            return DefDatabase<StatDef>.GetNamed("XylWillFallRate");
        }

        private static readonly InstructionMatcher Fixup_Interacted = new()
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
                        // statValue2 *= recipient.GetStatValue(StatDefOfWillFallRate());
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(typeof(Patch_InteractionWorker_EnslaveAttempt), nameof(Patch_InteractionWorker_EnslaveAttempt.StatDefOfWillFallRate)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                        new CodeInstruction(OpCodes.Mul),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(InteractionWorker_EnslaveAttempt.Interacted))]
        public static IEnumerable<CodeInstruction> Interacted_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_Interacted.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }
    }
}
