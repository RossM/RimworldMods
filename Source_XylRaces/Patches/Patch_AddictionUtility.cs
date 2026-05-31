namespace XylXenos.Patches;

[HarmonyPatch(typeof(AddictionUtility))]
public static class Patch_AddictionUtility
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
    public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(
        Pawn pawn,
        ChemicalDef chemicalDef,
        ref float effect)
    {
        if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
            effect = 0f;
        else
            effect *= pawn.GetStatValue(DefOf.XylDrugEffectMultiplier);
    }

    [Feature(typeof(DefModExtension_Chemical))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(AddictionUtility.CanBingeOnNow))]
    public static void CanBingeOnNow_Postfix(
        Pawn pawn,
        ChemicalDef chemical,
        DrugCategory drugCategory,
        ref bool __result)
    {
        if (!pawn.ChemicalIsAllowedByGenes(chemical))
            __result = false;
    }
}
