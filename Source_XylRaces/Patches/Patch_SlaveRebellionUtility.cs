using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(SlaveRebellionUtility))]
    public static class Patch_SlaveRebellionUtility
    {
        public const float DocileFactor = 4f;
        public const float NeverRebelThresholdDays = 120f;

        [Feature(nameof(GeneDefExtension_SlaveRebellion)), HarmonyPostfix, UsedImplicitly, HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
        public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
        {
            if (__result < 0)
                return;

            foreach (var defExt in pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_SlaveRebellion>())
                __result *= defExt.slaveRebellionMtbFactor;

            if (TryGetPawnRebellionThresholdDays(pawn, out float neverRebelThresholdDays))
            {
                if (__result >= neverRebelThresholdDays)
                {
                    __result = -1;
                    return;
                }
            }
        }

        private static bool TryGetPawnRebellionThresholdDays(Pawn pawn, out float neverRebelThresholdDays)
        {
            return pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_SlaveRebellion>()
                .Select(defExt => defExt.neverRebelThresholdDays).Where(x => x >= 0).TryMinBy(x => x, out neverRebelThresholdDays);
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

        [Feature(nameof(GeneDefExtension_SlaveRebellion)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
        public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
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

            foreach (var gene in pawn.GenesWithModExtension<GeneDefExtension_SlaveRebellion>()
                         .Where(gene => gene.Active))
            {
                var defExt = gene.def.GetModExtension<GeneDefExtension_SlaveRebellion>();

                if (defExt.slaveRebellionMtbFactor != 1)
                    stringBuilder.AppendLine(
                        $"{gene.def.LabelCap}: x{defExt.slaveRebellionMtbFactor.ToStringPercent()}");
            }

            if (initiateSlaveRebellionMtbDays < 0 && TryGetPawnRebellionThresholdDays(pawn, out float neverRebelThresholdDays))
            {
                var gene = pawn.GenesWithModExtension<GeneDefExtension_SlaveRebellion>().First(gene =>
                    gene.Active &&
                    gene.def.GetModExtension<GeneDefExtension_SlaveRebellion>().neverRebelThresholdDays ==
                    neverRebelThresholdDays);
                stringBuilder.AppendLine($"{gene.def.LabelCap}: " +
                                         "XylDocileThresholdReached".Translate(neverRebelThresholdDays));
            }

            string period = initiateSlaveRebellionMtbDays < 0
                ? "Never".TranslateSimple()
                : ((int)(initiateSlaveRebellionMtbDays * 60000f)).ToStringTicksToPeriod();
            stringBuilder.Append($"{"SuppressionFinalInterval".Translate()}: {period}");
        }
    }
}
