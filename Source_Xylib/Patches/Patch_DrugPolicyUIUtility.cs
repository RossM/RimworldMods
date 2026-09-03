namespace Xylib.Patches;

[HarmonyPatch(typeof(DrugPolicyUIUtility))]
internal static class Patch_DrugPolicyUIUtility
{
    [Feature(nameof(DefModExtension_GeneWithComps.showInDrugPolicies))]
    [Postfix]
    [Target(typeof(PawnUtility), nameof(PawnUtility.TryGetChemicalDependencyGene))]
    [PatchOptions(PatchOptions.AllowUnsafe)]
    public static void TryGetChemicalDependencyGene_Postfix(Pawn pawn, ref Gene? gene, ref bool __result)
    {
        if (!__result)
            __result = PatchHelpers.TryGetChemicalDependencyGene(pawn, out gene);
    }
}
