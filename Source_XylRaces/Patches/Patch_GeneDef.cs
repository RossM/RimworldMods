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
        [InfixPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
        [InfixPatch("GetDescriptionFull")]
        public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
        {
            var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
            if (extraDescriptions.Count == 0)
                return;

            __result = __result.NullOrEmpty() ? extraDescriptions : __result.Concat(extraDescriptions).ToList();
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
