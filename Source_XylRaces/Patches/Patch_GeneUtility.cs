namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneUtility))]
public static class Patch_GeneUtility
{
    [Feature(nameof(NotificationDefOf.PostSatisfyChemicalGenes))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GeneUtility.SatisfyChemicalGenes))]
    public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PostSatisfyChemicalGenes, pawn);
    }
}
