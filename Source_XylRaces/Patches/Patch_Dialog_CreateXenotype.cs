namespace XylXenos.Patches;

[HarmonyPatch(typeof(Dialog_CreateXenotype))]
public class Patch_Dialog_CreateXenotype
{
    [Feature(nameof(DefExt.showInXenotypeCreation))]
    [Feature(nameof(DefExt.geneType))]
    [InfixPostfix(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder))]
    [InfixPatch("DrawGenes")]
    public static void GenesInOrder_Postfix(Dialog_CreateXenotype __caller, ref List<GeneDef> __result)
    {
        __result = FilterGenes(__result, __caller.inheritable, __caller.ignoreRestrictions);
    }

    private static List<GeneDef> FilterGenes(List<GeneDef> genes, bool inheritable, bool ignoreRestrictions)
    {
        if (ignoreRestrictions)
            return genes;
        return genes.Where(g => PatchHelpers.GeneShouldBeVisible(g, inheritable ? GeneType.Endogene : GeneType.Xenogene)).ToList();
    }
}