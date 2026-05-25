using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(SlaveRebellionUtility))]
    public static class Patch_SlaveRebellionUtility
    {
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

        [Feature(nameof(GeneDefExt.slaveRebellionMtbFactor))]
        [Feature(nameof(GeneDefExt.slaveRebellionThresholdDays))]
        [HarmonyPostfix]
        [HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
        public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
        {
            if (__result < 0)
                return;

            var geneSet = pawn.GetComp<CompPawn_GeneSet>();
            __result *= geneSet.slaveRebellionMtbFactor;
            if (__result >= geneSet.slaveRebellionThresholdDays)
                __result = -1;
        }

        [Feature(nameof(GeneDefExt.slaveRebellionMtbFactor))]
        [Feature(nameof(GeneDefExt.slaveRebellionThresholdDays))]
        [HarmonyTranspiler]
        [HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
        public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetSlaveRebellionMtbCalculationExplanation.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        private static void FinishExplanation(StringBuilder stringBuilder, Pawn pawn)
        {
            if (pawn == null)
                return;

            float initiateSlaveRebellionMtbDays = SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn);

            foreach (var def in pawn.ActiveExtendedGeneDefs())
            {
                if (def.slaveRebellionMtbFactor != 1)
                    stringBuilder.AppendLine($"{def.LabelCap}: x{def.slaveRebellionMtbFactor.ToStringPercent()}");
            }

            if (initiateSlaveRebellionMtbDays < 0)
            {
                var def = pawn.ActiveExtendedGeneDefs().OrderBy(def => def.slaveRebellionThresholdDays).FirstOrDefault();
                if (def is { slaveRebellionThresholdDays: < float.MaxValue })
                    stringBuilder.AppendLine($"{def.LabelCap}: {"XylDocileThresholdReached".Translate(def.slaveRebellionThresholdDays)}");
            }

            string period = initiateSlaveRebellionMtbDays < 0
                ? "Never".TranslateSimple()
                : ((int)(initiateSlaveRebellionMtbDays * GenDate.TicksPerDay)).ToStringTicksToPeriod();
            stringBuilder.Append($"{"SuppressionFinalInterval".Translate()}: {period}");
        }
    }
}
