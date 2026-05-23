using HarmonyLib;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnCapacityUtility))]
    public static class Patch_PawnCapacityUtility

    {
        [Feature(typeof(HediffWithCompsExt))]
        [InfixWrapper(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch(nameof(PawnCapacityUtility.CalculatePartEfficiency))]
        public static float HediffStage_partEfficiencyOffset_Wrapper(HediffStage __instance, HediffSet diffSet)
        {
            var hediff = diffSet.hediffs.FirstOrDefault(hediff => hediff.CurStage == __instance);
            if (hediff is HediffWithCompsExt ext)
                return ext.PartEfficiencyOffset;

            return __instance.partEfficiencyOffset;
        }
    }
}
