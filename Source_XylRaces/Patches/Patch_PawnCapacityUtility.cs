namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnCapacityUtility))]
public static class Patch_PawnCapacityUtility

{
    [Feature(typeof(HediffWithCompsExt))]
    [InfixPostfix(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
    [InfixPatch(nameof(PawnCapacityUtility.CalculatePartEfficiency))]
    public static void HediffStage_partEfficiencyOffset_Postfix(HediffStage __instance, HediffSet diffSet, ref float __result)
    {
        var hediff = diffSet.hediffs.FirstOrDefault(hediff => hediff.CurStage == __instance);
        if (hediff is HediffWithCompsExt ext)
            __result = ext.PartEfficiencyOffset;
    }
}