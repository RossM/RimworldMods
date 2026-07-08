namespace Xylib.Patches;

[HarmonyPatch(typeof(HealthUtility))]
internal static class Patch_HealthUtility
{
    [Feature(nameof(Config.resistanceStatByHediff))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HealthUtility.AdjustSeverity))]
    public static void AdjustSeverity_Prefix(Pawn pawn, HediffDef hdDef, ref float sevOffset)
    {
        if (sevOffset < 0)
            return;

        float resistance = PatchHelpers.GetHediffResistance(pawn, hdDef);
        float factor = Mathf.Max(1f - resistance, 0f);

        sevOffset *= factor;
    }
}
