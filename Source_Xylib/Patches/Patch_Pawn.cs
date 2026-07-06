namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn))]
internal static class Patch_Pawn
{
    // Note: This patch is performance-sensitive
    [Feature(typeof(GeneCompProperties_RaceModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
    public static void BodySize_Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.GeneTracker_Xylib is { } geneTracker)
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
    [Feature(typeof(GeneCompProperties_RaceModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
    public static void HealthScale_Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.GeneTracker_Xylib is { } geneTracker)
            __result *= geneTracker.healthScaleFactor;
    }

    private static bool dyingPawnIsMutant;

    [Feature(nameof(EventDefOf.PostMutated))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn.Kill))]
    public static void Kill_Prefix(Pawn __instance)
    {
        dyingPawnIsMutant = __instance.mutant != null;
    }

    [Feature(nameof(EventDefOf.PostPawnKilled))]
    [Feature(nameof(EventDefOf.PostMutated))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.Kill))]
    public static void Kill_Postfix(Pawn __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostPawnKilled, __instance);
        if (dyingPawnIsMutant && __instance.mutant == null)
            EventManager.Instance.Notify(EventDefOf.PostMutated, __instance);
    }
}
