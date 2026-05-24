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
        [InfixPostfix(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch(typeof(HediffStatsUtility), "<SpecialDisplayStats>:MoveNext")]
        public static void HediffStage_partEfficiencyOffset_Postfix(HediffStage __instance, Hediff instance, ref float __result)
        {
            if (instance is HediffWithCompsExt ext)
                __result = ext.PartEfficiencyOffset;
        }
    }
}
