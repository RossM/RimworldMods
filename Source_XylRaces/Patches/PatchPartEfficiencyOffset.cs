using System.Linq;
using HarmonyLib;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchPartEfficiencyOffset
    {
        [Feature(typeof(HediffWithCompsExt))]
        [InfixPrefix(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch(typeof(HediffStatsUtility), "<SpecialDisplayStats>:MoveNext")]
        public static bool HediffStage_partEfficiencyOffset_Prefix(HediffStage __instance, Hediff instance, out float __result)
        {
            if (instance is HediffWithCompsExt ext)
            {
                __result = ext.PartEfficiencyOffset;
                return false;
            }

            __result = default;
            return true;
        }
    }
}
