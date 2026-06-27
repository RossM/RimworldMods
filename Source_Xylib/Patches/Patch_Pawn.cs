namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn))]
public static class Patch_Pawn
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

    [Feature(nameof(DefModExtension_GeneWithComps.hasPsycast))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        if (__instance.GeneTracker_GeneWithComps is { } geneTracker)
            __result |= geneTracker.hasPsycast;
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
