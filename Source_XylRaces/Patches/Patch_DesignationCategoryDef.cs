using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;
using static Verse.DesignationCategoryDef;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DesignationCategoryDef))]
    public static class Patch_DesignationCategoryDef
    {
        [Feature(typeof(GeneDefExtension_Designator))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
        public static void ResolvedAllowedDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            GeneHelpers.AddDesignators(__instance, ref __result);
        }

        [Feature(typeof(GeneDefExtension_Designator))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
        public static void AllResolvedAndIdeoDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            GeneHelpers.AddDesignators(__instance, ref __result);
        }
    }
}
