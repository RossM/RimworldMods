namespace XylXenos.Patches;

[HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
public static class Patch_JobGiver_OptimizeApparel
{
    [Feature(typeof(Gene_Flight))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static void ApparelScoreRaw_Postfix(Pawn pawn, Apparel ap, ref float __result)
    {
        if (pawn.HasActiveGeneOfType<Gene_Flight>() && !Gene_Flight.ApparelAllowsFlight(ap.def))
            __result = -10f;
    }
}
