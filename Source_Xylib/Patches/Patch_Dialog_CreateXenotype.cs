namespace Xylib.Patches;

[HarmonyPatch(typeof(Dialog_CreateXenotype))]
internal static class Patch_Dialog_CreateXenotype
{
    [Feature(nameof(DefModExtension_GeneWithComps.showInXenotypeCreation))]
    [Feature(nameof(DefModExtension_GeneWithComps.geneType))]
    [Postfix] [Inner(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder))]
    [Target("DrawGenes")]
    public static void GenesInOrder_Postfix(ref List<GeneDef> __result, bool ___inheritable, bool ___ignoreRestrictions)
    {
        __result = PatchHelpers.FilterGenes(__result, ___inheritable, ___ignoreRestrictions);
    }
}
