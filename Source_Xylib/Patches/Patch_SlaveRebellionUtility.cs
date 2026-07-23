namespace Xylib.Patches;

[HarmonyPatch(typeof(SlaveRebellionUtility))]
internal static class Patch_SlaveRebellionUtility
{
    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [Postfix]
    [Target("InitiateSlaveRebellionMtbDaysHelper")]
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
    [InnerPostfix(typeof(StringBuilder), memberType: MemberType.Constructor, parameterTypes: [])]
    [Target("GetSlaveRebellionMtbCalculationExplanation")]
    public static void StringBuilder_ctor_Postfix(StringBuilder __result, [State] out StringBuilder sb)
    {
        sb = __result;
    }

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [InnerPostfixConstant("SuppressionFinalInterval")]
    [Target("GetSlaveRebellionMtbCalculationExplanation")]
    public static void SuppressionFinalInterval_Postfix(Pawn? pawn, [State] StringBuilder sb)
    {
        PatchHelpers.AddSlaveRebellionMtbFactorExplanation(sb, pawn);
    }

    [Feature(nameof(XStatDefOf.XylSlaveRebellionMtbFactor))]
    [InnerPostfix(typeof(GenDate), nameof(GenDate.ToStringTicksToPeriod))]
    [Target("GetSlaveRebellionMtbCalculationExplanation")]
    public static void ToStringTicksToPeriod_Postfix(int numTicks, ref string __result)
    {
        if (numTicks < 0)
            __result = "Never".Translate();
    }
}
