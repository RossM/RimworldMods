namespace Xylib.Patches;

[Patch(typeof(DrugPolicyUIUtility))]
internal static class Patch_DrugPolicyUIUtility
{
    [Feature(nameof(DefModExtension_GeneWithComps.showInDrugPolicies))]
    [Postfix]
    [Inner(typeof(GenText), nameof(GenText.Truncate), typeof(string), typeof(float), typeof(Dictionary<string, string>))]
    [Target(nameof(DrugPolicyUIUtility.DoAssignDrugPolicyButtons))]
    public static void TryGetChemicalDependencyGene_Postfix(Pawn pawn, ref string __result)
    {
        if (__result.Contains('('))
            return;
        if (PatchHelpers.TryGetChemicalDependencyGene(pawn, out var gene))
            __result += $" ({gene.Label})";
    }
}
