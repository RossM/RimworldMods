namespace Xylib.Patches;

[HarmonyPatch(typeof(HealthUtility))]
internal static class Patch_HealthUtility
{
    [Feature(nameof(XStatDefOf.XylHypothermiaProgressionFactor))]
    [Feature(nameof(XStatDefOf.XylMalnutritionProgressionFactor))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HealthUtility.AdjustSeverity))]
    public static void AdjustSeverity_Prefix(Pawn pawn, HediffDef hdDef, ref float sevOffset)
    {
        if (hdDef == HediffDefOf.Hypothermia)
            sevOffset *= pawn.GetStatValue(XStatDefOf.XylHypothermiaProgressionFactor);
        if (hdDef == HediffDefOf.Malnutrition)
            sevOffset *= pawn.GetStatValue(XStatDefOf.XylMalnutritionProgressionFactor);
    }
}
