using System.Collections.Generic;
using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DesignationCategoryDef))]
    public static class Patch_DesignationCategoryDef
    {
        [Feature(nameof(GeneDefExt.addDesignators))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
        public static void ResolvedAllowedDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            GeneHelpers.AddDesignators(__instance, ref __result);
        }

        [Feature(nameof(GeneDefExt.addDesignators))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
        public static void AllResolvedAndIdeoDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            GeneHelpers.AddDesignators(__instance, ref __result);
        }
    }
}
