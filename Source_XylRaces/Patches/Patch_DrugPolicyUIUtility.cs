using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DrugPolicyUIUtility))]
    public static class Patch_DrugPolicyUIUtility
    {
        [Feature(typeof(GeneDefExtension_Chemicals))]
        [WrappedMember(typeof(PawnUtility), nameof(PawnUtility.TryGetChemicalDependencyGene))]
        [InfixPatch(nameof(DrugPolicyUIUtility.DoAssignDrugPolicyButtons))]
        public static bool TryGetChemicalDependencyGene_Wrapper(Pawn pawn, out Gene gene)
        {
            if (PawnUtility.TryGetChemicalDependencyGene(pawn, out var chemicalDependency))
            {
                gene = chemicalDependency;
                return true;
            }

            return GeneHelpers.TryGetChemicalDependencyGene(pawn, out gene);
        }
    }
}
