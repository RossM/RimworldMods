using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(GeneUtility))]
    public static class Patch_GeneUtility
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(GeneUtility.SatisfyChemicalGenes))]
        public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
        {
            foreach (var gene in pawn.ActiveGenesOfType<DietDependency>())
            {
                gene.Reset();
            }
        }
    }
}
