using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GeneUtility))]
    public static class Patch_GeneUtility
    {
        [Feature(nameof(DietDependency))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GeneUtility.SatisfyChemicalGenes))]
        public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PostSatisfyGenes, pawn);
        }
    }
}
