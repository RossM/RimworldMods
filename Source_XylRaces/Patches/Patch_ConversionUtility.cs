using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using UnityEngine;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(ConversionUtility))]
    public static class Patch_ConversionUtility
    {
        private static readonly InstructionMatcher Fixup_ConversionPowerFactor_MemesVsTraits = new()
        {
            Rules =
            {
                // I would love to use a match against OffsetAgainstIdeo but that's a local function

                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Ldc_R4, -0.4f),
                        CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Max), [typeof(float), typeof(float)]),
                    ],
                    Output =
                    [
                        // + OffsetFromXenotype(initiator, recipient, false, sb)
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(0),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(() => PatchHelpers.ConversionPowerFactor_OffsetFromXenotype),
                        new CodeInstruction(OpCodes.Add),
                        // + OffsetFromXenotype(recipient, recipient, true, sb)
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.LoadArgument(0),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(() => PatchHelpers.ConversionPowerFactor_OffsetFromXenotype),
                        new CodeInstruction(OpCodes.Add),
                    ]
                }
            }
        };

        [Feature(typeof(XenotypeDefExtension))]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ConversionUtility.ConversionPowerFactor_MemesVsTraits))]
        public static IEnumerable<CodeInstruction> ConversionPowerFactor_MemesVsTraits_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_ConversionPowerFactor_MemesVsTraits.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }
    }
}
