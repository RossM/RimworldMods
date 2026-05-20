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
            AddDesignators(__instance, ref __result);
        }

        [Feature(typeof(GeneDefExtension_Designator))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
        public static void AllResolvedAndIdeoDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            AddDesignators(__instance, ref __result);
        }

        private static void AddDesignators(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            HashSet<Designator> geneDesignators = new();

            foreach (var defExtension_designator in Faction.OfPlayer.GetPawns()
                         .SelectMany(pawn => pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()))
            {
                geneDesignators.AddRange(defExtension_designator.addDesignators.Where(def => def.designationCategory == __instance)
                    .Select(GetCachedDesignator));
            }

            if (geneDesignators.Any())
                __result = __result.Concat(geneDesignators);

            Designator GetCachedDesignator(BuildableDef def)
            {
                BuildablePreceptBuilding key = new BuildablePreceptBuilding(def, null);
                if (!__instance.ideoBuildingDesignatorsCached.TryGetValue(key, out var value))
                {
                    value = new Designator_Build(def);
                    __instance.ideoBuildingDesignatorsCached[key] = value;
                }

                return value;
            }
        }
    }
}
