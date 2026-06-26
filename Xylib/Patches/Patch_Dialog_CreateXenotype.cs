namespace Xylib.Patches;

[HarmonyPatch(typeof(Dialog_CreateXenotype))]
public class Patch_Dialog_CreateXenotype
{
    [Feature(nameof(DefModExtension_GeneWithComps.showInXenotypeCreation))]
    [Feature(nameof(DefModExtension_GeneWithComps.geneType))]
    [InfixPostfix(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder))]
    [InfixPatch("DrawGenes")]
    public static void GenesInOrder_Postfix(ref List<GeneDef> __result, bool ___inheritable, bool ___ignoreRestrictions)
    {
        __result = FilterGenes(__result, ___inheritable, ___ignoreRestrictions);
    }

    private static List<GeneDef> FilterGenes(List<GeneDef> genes, bool inheritable, bool ignoreRestrictions)
    {
        if (ignoreRestrictions)
            return genes;
        return genes.Where(g => Xylib.PatchHelpers.GeneShouldBeVisible(g, inheritable ? GeneType.Endogene : GeneType.Xenogene)).ToList();
    }
}
