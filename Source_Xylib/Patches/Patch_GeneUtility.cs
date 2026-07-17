namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneUtility))]
internal static class Patch_GeneUtility
{
    [Feature(nameof(EventDefOf.PostSatisfyChemicalGenes))]
    [Postfix]
    [Target(nameof(GeneUtility.SatisfyChemicalGenes))]
    public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostSatisfyChemicalGenes, pawn);
    }
}
