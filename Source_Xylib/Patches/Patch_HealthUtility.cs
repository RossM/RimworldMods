namespace Xylib.Patches;

[HarmonyPatch(typeof(HealthUtility))]
internal static class Patch_HealthUtility
{
    [Feature(nameof(XStatDefOf.XylBloodLossResistance))]
    [Feature(nameof(XStatDefOf.XylDrugOverdoseResistance))]
    [Feature(nameof(XStatDefOf.XylHeatstrokeResistance))]
    [Feature(nameof(XStatDefOf.XylHypothermiaResistance))]
    [Feature(nameof(XStatDefOf.XylMalnutritionResistance))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HealthUtility.AdjustSeverity))]
    public static void AdjustSeverity_Prefix(Pawn pawn, HediffDef hdDef, ref float sevOffset)
    {
        float resistance = PatchHelpers.GetHediffResistance(pawn, hdDef);
        float factor = Mathf.Max(1f - resistance, 0f);

        sevOffset *= factor;
    }
}
