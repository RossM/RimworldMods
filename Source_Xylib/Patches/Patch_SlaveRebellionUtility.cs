using System.Reflection;
using System.Reflection.Emit;

namespace Xylib.Patches;

[HarmonyPatch(typeof(SlaveRebellionUtility))]
internal static class Patch_SlaveRebellionUtility
{
    private static readonly InstructionMatcher.Rule Rule_AddSlaveRebellionMtbFactorExplanation = new()
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
            CodeInstruction.Call(() => PatchHelpers.AddSlaveRebellionMtbFactorExplanation),
        ]
    };

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [HarmonyTranspiler]
    [HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
    public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        return InstructionMatcher.MatchAndReplace([Rule_AddSlaveRebellionMtbFactorExplanation], method, instructions, generator);
    }

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [HarmonyPostfix]
    [HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
    public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
    {
        if (__result < 0)
            return;

        var geneTracker = pawn.GeneTracker_Xylib;
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
}
