using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GeneDef))]
    public static class Patch_GeneDef
    {
        [Feature(typeof(GeneDefExtension))]
        [InfixWrapper(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
        [InfixPatch("GetDescriptionFull")]
        public static List<string> GeneDef_customEffectDescriptions_Wrapper(GeneDef __instance)
        {
            return __instance.GetGeneEffectDescriptions().ToList();
        }

        [Feature(typeof(GeneDefExtension))]
        [HarmonyPostfix]
        [HarmonyPatch("SpecialDisplayStats")]
        public static void SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            var extraStats = __instance.GetGeneSpecialDisplayStats().ToList();
            if (extraStats.Count > 0)
                __result = __result.Concat(extraStats);
        }
    }
}
