namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneUtility))]
public static class Patch_GeneUtility
{
    [Feature(typeof(Hediff_DietDependency))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GeneUtility.SatisfyChemicalGenes))]
    public static void SatisfyChemicalGenes_Postfix(Pawn pawn)
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostSatisfyGenes, pawn);
    }
}
