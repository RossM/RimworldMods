using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
    public static class Patch_InteractionWorker_RecruitAttempt
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly]
            public static StatDef XylResistanceFallRate;
        }

        public static StatDef StatDefOfResistanceFallRate()
        {
            return Defs.XylResistanceFallRate;
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
                        // statValue2 *= recipient.GetStatValue(StatDefOfResistanceFallRate());
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(typeof(Patch_InteractionWorker_RecruitAttempt), nameof(StatDefOfResistanceFallRate)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                        new CodeInstruction(OpCodes.Mul),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
        public static IEnumerable<CodeInstruction> Interacted_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_Interacted.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}.{1}: {2}", MethodBase.GetCurrentMethod()?.DeclaringType?.Name, MethodBase.GetCurrentMethod()?.Name, reason));
            return instructionsList;
        }
    }
}
