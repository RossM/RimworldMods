using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DrugPolicyUIUtility))]
    public static class Patch_DrugPolicyUIUtility
    {
        [Feature(nameof(GeneDefExt.showInDrugPolicies))]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TryGetChemicalDependencyGene))]
        public static void TryGetChemicalDependencyGene_Postfix(Pawn pawn, ref Gene gene, ref bool __result)
        {
            if (__result == false)
                __result = PatchHelpers.TryGetChemicalDependencyGene(pawn, out gene);
        }
    }
}
