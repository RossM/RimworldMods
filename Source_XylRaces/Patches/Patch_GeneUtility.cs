namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneUtility))]
public static class Patch_GeneUtility
{
    [Feature(nameof(EventDefOf.PostSatisfyChemicalGenes))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GeneUtility.SatisfyChemicalGenes))]
    public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostSatisfyChemicalGenes, pawn);
    }
}
