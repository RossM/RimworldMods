namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_GeneTracker))]
public static class Patch_Pawn_GeneTracker
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Feature(nameof(XStatDefOf.XylGlobalAddictionChanceFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
    public static void AddictionChanceFactor_Postfix(Pawn_GeneTracker __instance, ChemicalDef chemical, ref float __result)
    {
        if (!__instance.pawn.ChemicalIsAllowedByGenes(chemical))
            __result = 0f;
        else
            __result *= __instance.pawn.GetStatValue(XStatDefOf.XylGlobalAddictionChanceFactor);
    }

    [Feature(nameof(EventDefOf.PostGenesChanged))]
    [HarmonyPostfix]
    [HarmonyPatch("Notify_GenesChanged")]
    public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostGenesChanged, __instance.pawn);
    }
}
