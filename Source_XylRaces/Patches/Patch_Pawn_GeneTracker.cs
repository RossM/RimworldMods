namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_GeneTracker))]
public static class Patch_Pawn_GeneTracker
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Feature(nameof(DefOf.XylGlobalAddictionChanceFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
    public static void AddictionChanceFactor_Postfix(Pawn_GeneTracker __instance, ChemicalDef chemical, ref float __result)
    {
        if (!__instance.pawn.ChemicalIsAllowedByGenes(chemical))
            __result = 0f;
        else
            __result *= __instance.pawn.GetStatValue(DefOf.XylGlobalAddictionChanceFactor);
    }

    [Feature(nameof(NotificationDefOf.PostGenesChanged))]
    [HarmonyPostfix]
    [HarmonyPatch("Notify_GenesChanged")]
    public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PostGenesChanged, __instance.pawn);
    }
}
