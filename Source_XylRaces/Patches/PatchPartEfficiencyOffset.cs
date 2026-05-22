using System.Linq;
using HarmonyLib;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchPartEfficiencyOffset
    {
        public static AccessTools.FieldRef<object, Hediff> hediffGetter = AccessTools.FieldRefAccess<Hediff>(
            AccessTools.InnerTypes(typeof(HediffStatsUtility))
                .First(type => type.Name.Contains("<SpecialDisplayStats>")),
            "<>3__instance");

        [Feature(typeof(HediffWithCompsExt))]
        [WrappedMember(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset))]
        [InfixPatch(typeof(HediffStatsUtility), "<SpecialDisplayStats>:MoveNext")]
        public static float HediffStage_partEfficiencyOffset_Wrapper(HediffStage __instance, object __caller)
        {
            var hediff = hediffGetter(__caller);
            if (hediff is HediffWithCompsExt ext)
                return ext.PartEfficiencyOffset;

            return __instance.partEfficiencyOffset;
        }
    }
}
