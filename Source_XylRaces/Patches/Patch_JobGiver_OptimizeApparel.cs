namespace XylXenos.Patches;

[HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
public static class Patch_JobGiver_OptimizeApparel
{
    [Feature(typeof(GeneComp_Flight))]
    [Postfix]
    [Target(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static void ApparelScoreRaw_Postfix(Pawn pawn, Apparel ap, ref float __result)
    {
        if (pawn.HasActiveGeneWithComp<GeneComp_Flight>() && !GeneComp_Flight.ApparelAllowsFlight(ap.def))
            __result = -10f;
    }
}
