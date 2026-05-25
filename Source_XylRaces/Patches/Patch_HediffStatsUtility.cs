using HarmonyLib;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HediffStatsUtility))]
    public static class Patch_HediffStatsUtility
    {
        [Feature(typeof(HediffWithCompsExt))]
        [InfixPostfix(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch("<SpecialDisplayStats>:MoveNext")]
        public static void HediffStage_partEfficiencyOffset_Postfix(HediffStage __instance, Hediff instance, ref float __result)
        {
            if (instance is HediffWithCompsExt ext)
                __result = ext.PartEfficiencyOffset;
        }
    }
}
