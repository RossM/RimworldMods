namespace Xylib.Patches;

[HarmonyPatch(typeof(AddictionUtility))]
internal static class Patch_AddictionUtility
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Postfix]
    [Target(nameof(AddictionUtility.CanBingeOnNow))]
    public static void CanBingeOnNow_Postfix(
        Pawn pawn,
        ChemicalDef chemical,
        ref bool __result)
    {
        if (!pawn.ChemicalIsAllowedByGenes(chemical))
            __result = false;
    }

    [Feature(typeof(DefModExtension_Chemical))]
    [Feature(nameof(XStatDefOf.XylDrugEffectMultiplier))]
    [Postfix]
    [Target(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
    public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(
        Pawn pawn,
        ChemicalDef chemicalDef,
        ref float effect)
    {
        if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
            effect = 0f;
        else
            effect *= pawn.GetStatValue(XStatDefOf.XylDrugEffectMultiplier);
    }
}
