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
        __result *= __instance.GeneTracker?.bodySizeFactor ?? 1f;
    }

    [Feature(nameof(DefModExtension_Gene.hasPsycast))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        __result |= __instance.HasActivePsycastGene;
    }

    [Feature(nameof(DefModExtension_Gene.healthScaleFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
    public static void HealthScale_Postfix(Pawn __instance, ref float __result)
    {
        __result *= __instance.GeneTracker?.healthScaleFactor ?? 1f;
    }

    [Feature(nameof(NotificationDefOf.PostPawnKilled))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.Kill))]
    public static void Kill_Postfix(Pawn __instance)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PostPawnKilled, __instance);
    }
}
