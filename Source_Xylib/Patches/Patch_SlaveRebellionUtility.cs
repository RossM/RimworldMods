using System.Reflection;
using System.Reflection.Emit;

namespace Xylib.Patches;

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
                Mode = InstructionMatcher.OutputMode.InsertBefore,
                Pattern =
                [
                    CodeInstruction.LoadLocal(0),
                    new CodeInstruction(OpCodes.Ldstr, "{0}: {1}"),
                    new CodeInstruction(OpCodes.Ldstr, "SuppressionFinalInterval"),
                ],
                Output =
                [
                    // Load stringBuilder
                    CodeInstruction.LoadLocal(0),
                    // Load pawn
                    CodeInstruction.LoadArgument(0),
                    // Call FinishExplanation
                    CodeInstruction.Call(() => InsertExplanation),
                ]
            }
        }
    };

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [HarmonyTranspiler]
    [HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
    [HarmonyDebug]
    public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        Fixup_GetSlaveRebellionMtbCalculationExplanation.MatchAndReplace(method, ref instructionsList, generator);
        return instructionsList;
    }

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [HarmonyPostfix]
    [HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
    public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
    {
        if (__result < 0)
            return;

        var geneTracker = pawn.GeneTracker_GeneWithComps;
        if (geneTracker == null)
            return;

        __result *= pawn.GetStatValue(XStatDefOf.XylSlaveRebellionMtbFactor);
    }

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [InfixPostfix(typeof(GenDate), nameof(GenDate.ToStringTicksToPeriod))]
    [InfixPatch("GetSlaveRebellionMtbCalculationExplanation")]
    public static void ToStringTicksToPeriod_Postfix(int numTicks, ref string __result)
    {
        if (numTicks < 0)
            __result = "Never".Translate();
    }

    private static void InsertExplanation(StringBuilder stringBuilder, Pawn pawn)
    {
        if (pawn == null)
            return;

        StatRequest statRequest = StatRequest.For(pawn);
        float baseValueFor = XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetBaseValueFor(statRequest);
        ToStringNumberSense toStringNumberSense = XStatDefOf.XylSlaveRebellionMtbFactor.toStringNumberSense;
        XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetOffsetsAndFactorsExplanation(statRequest, stringBuilder, baseValueFor);
        XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetAdditionalOffsetsAndFactorsExplanation(statRequest, toStringNumberSense,
            stringBuilder);
    }
}
