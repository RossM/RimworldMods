namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_GeneTracker))]
internal static class Patch_Pawn_GeneTracker
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Feature(nameof(XStatDefOf.XylGlobalAddictionChanceFactor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
    public static void AddictionChanceFactor_Postfix(ChemicalDef chemical, ref float __result, Pawn ___pawn)
    {
        if (!___pawn.ChemicalIsAllowedByGenes(chemical))
            __result = 0f;
        else
            __result *= ___pawn.GetStatValue(XStatDefOf.XylGlobalAddictionChanceFactor);
    }

    [Feature(nameof(EventDefOf.PostGenesChanged))]
    [HarmonyPostfix]
    [HarmonyPatch("Notify_GenesChanged")]
    public static void Notify_GenesChanged_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostGenesChanged, ___pawn);
    }
}
