namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn))]
internal static class Patch_Pawn
{
    // Note: This patch is performance-sensitive
    [Feature(nameof(DefModExtension_GeneWithComps.bodySizeFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
    public static void BodySize_Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.GeneTracker_GeneWithComps is { } geneTracker)
            __result *= geneTracker.bodySizeFactor;
    }

    [Feature(nameof(EventDefOf.InPawnExposeData))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.ExposeData))]
    public static void ExposeData_Postfix(Pawn __instance)
    {
        Scribe.EnterNode("Xylib_PawnData");
        EventManager.Instance.Notify(EventDefOf.InPawnExposeData, __instance);
        Scribe.ExitNode();
    }

    // Note: This patch is performance-sensitive
    [Feature(nameof(DefModExtension_GeneWithComps.healthScaleFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
    public static void HealthScale_Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.GeneTracker_GeneWithComps is { } geneTracker)
            __result *= geneTracker.healthScaleFactor;
    }

    [Feature(nameof(EventDefOf.PostPawnKilled))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.Kill))]
    public static void Kill_Postfix(Pawn __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostPawnKilled, __instance);
    }
}
