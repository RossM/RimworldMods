using HarmonyLib;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnCapacityUtility))]
    public static class Patch_PawnCapacityUtility

    {
        [Feature(typeof(HediffWithCompsExt))]
        [InfixPrefix(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch(nameof(PawnCapacityUtility.CalculatePartEfficiency))]
        public static bool HediffStage_partEfficiencyOffset_Prefix(HediffStage __instance, HediffSet diffSet, out float __result)
        {
            var hediff = diffSet.hediffs.FirstOrDefault(hediff => hediff.CurStage == __instance);
            if (hediff is HediffWithCompsExt ext)
            {
                __result = ext.PartEfficiencyOffset;
                return false;
            }

            __result = default;
            return true;
        }
    }
}
