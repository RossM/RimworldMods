namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneUtility))]
public static class Patch_GeneUtility
{
    [Feature("TODO")]
    [Postfix] [Inner(typeof(GenCollection), nameof(GenCollection.SortBy),
        genericTypes: [typeof(GeneDef), typeof(float), typeof(string), typeof(float)])]
    [Target(typeof(GeneUtility), "get_GenesInOrder")]
    public static void SortBy_Postfix(List<GeneDef> list)
    {
        PatchHelpers.SortColorGenes(list);
    }
}
