namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneUtility))]
public static class Patch_GeneUtility
{
    [Feature("TODO")]
    [InfixPostfix(typeof(GenCollection), nameof(GenCollection.SortBy),
        genericTypes: [typeof(GeneDef), typeof(float), typeof(string), typeof(float)])]
    [InfixPatch(typeof(GeneUtility), "get_GenesInOrder")]
    public static void SortBy_Postfix(List<GeneDef> list)
    {
        PatchHelpers.SortColorGenes(list);
    }
}
