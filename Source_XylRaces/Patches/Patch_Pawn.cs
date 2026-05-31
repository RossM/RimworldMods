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

    [Feature(nameof(DefModExtension_Gene.healthScaleFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
    public static void HealthScale_Postfix(Pawn __instance, ref float __result)
    {
        __result *= __instance.GeneTracker?.healthScaleFactor ?? 1f;
    }

    [Feature(nameof(DefModExtension_Gene.hasPsycast))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        __result |= __instance.HasActivePsycastGene;
    }

    [Feature(typeof(GeneTracker))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.Discard))]
    public static void Discard_Postfix(Pawn __instance)
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostDiscard, __instance);
    }

    [Feature(typeof(GeneTracker))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.PostMake))]
    public static void PostMake_Postfix(Pawn __instance)
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostPostMake, __instance);
    }
}
