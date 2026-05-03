using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(SlaveRebellionUtility))]
    public static class Patch_SlaveRebellionUtility
    {
        public const float DocileFactor = 4f;
        public const float NeverRebelThresholdDays = 120f;

        [DefOf]
        public static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
            public static GeneDef XylDocile;
        }

        [Feature(nameof(Defs.XylDocile)), HarmonyPostfix, UsedImplicitly, HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
        public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
        {
            using (new ProfileBlock())
            {
                if (__result < 0)
                    return;
                if (pawn.HasActiveGene(Defs.XylDocile))
                {
                    __result *= DocileFactor;
                    if (__result > NeverRebelThresholdDays)
                        __result = -1;
                }
            }
        }

        private static readonly InstructionMatcher Fixup_GetSlaveRebellionMtbCalculationExplanation = new()
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
                        new CodeInstruction(OpCodes.Ldstr, "{0}: {1}"),
                        new CodeInstruction(OpCodes.Ldstr, "SuppressionFinalInterval"),
                        CodeInstruction.Call(typeof(Translator), nameof(Translator.Translate), [typeof(string)]),
                        new CodeInstruction(OpCodes.Box, typeof(TaggedString)),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(typeof(SlaveRebellionUtility), nameof(SlaveRebellionUtility.InitiateSlaveRebellionMtbDays)),
                        new CodeInstruction(OpCodes.Ldc_R4, 60000),
                        new CodeInstruction(OpCodes.Mul),
                        new CodeInstruction(OpCodes.Conv_I4),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        CodeInstruction.Call(typeof(GenDate), nameof(GenDate.ToStringTicksToPeriod)),
                        CodeInstruction.Call(typeof(string), nameof(string.Format), [typeof(string), typeof(object), typeof(object)]), 
                        CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.Append), [typeof(string)]),
                        new CodeInstruction(OpCodes.Pop),
                    ],
                    Output =
                    [
                        // Load stringBuilder
                        CodeInstruction.LoadLocal(0),
                        // Load pawn
                        CodeInstruction.LoadArgument(0), 
                        // Call FinishExplanation
                        CodeInstruction.Call(() => FinishExplanation), 
                    ]
                }
            }
        };

        [Feature(nameof(Defs.XylDocile)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
        public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetSlaveRebellionMtbCalculationExplanation.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        private static void FinishExplanation(StringBuilder stringBuilder, Pawn pawn)
        {
            using (new ProfileBlock())
            {
                float initiateSlaveRebellionMtbDays = SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn);

                if (pawn.HasActiveGene(Defs.XylDocile))
                {
                    stringBuilder.AppendLine($"{Defs.XylDocile.LabelCap}: x{DocileFactor.ToStringPercent()}");

                    if (initiateSlaveRebellionMtbDays < 0)
                        stringBuilder.AppendLine($"{Defs.XylDocile.LabelCap}: " +
                                                 "XylDocileThresholdReached".Translate(NeverRebelThresholdDays));
                }

                string period = initiateSlaveRebellionMtbDays < 0
                    ? "Never".TranslateSimple()
                    : ((int)(initiateSlaveRebellionMtbDays * 60000f)).ToStringTicksToPeriod();
                stringBuilder.Append($"{"SuppressionFinalInterval".Translate()}: {period}");
            }
        }
    }
}
