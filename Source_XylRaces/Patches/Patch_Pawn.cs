namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn))]
public static class Patch_Pawn
{
    // Note: This patch is performance-sensitive
    [Feature(nameof(DefModExtension_Gene.bodySizeFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
    public static void BodySize_Postfix(Pawn __instance, ref float __result)
    {
        GeneTracker geneTracker = __instance.GeneTracker;
        if (geneTracker != null)
            __result *= geneTracker.bodySizeFactor;
    }

    [Feature(nameof(DefModExtension_Gene.hasPsycast))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        GeneTracker geneTracker = __instance.GeneTracker;
        if (geneTracker != null)
            __result |= geneTracker.hasPsycast;
    }

    // Note: This patch is performance-sensitive
    [Feature(nameof(DefModExtension_Gene.healthScaleFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
    public static void HealthScale_Postfix(Pawn __instance, ref float __result)
    {
        GeneTracker geneTracker = __instance.GeneTracker;
        if (geneTracker != null)
            __result *= geneTracker.healthScaleFactor;
    }

    [Feature(nameof(NotificationDefOf.PostPawnKilled))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.Kill))]
    public static void Kill_Postfix(Pawn __instance)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PostPawnKilled, __instance);
    }
}
