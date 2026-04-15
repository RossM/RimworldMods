using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using XylRacesCore.Genes;
using static Verse.DesignationCategoryDef;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Verse.DesignationCategoryDef))]
    public static class Patch_DesignationCategoryDef
    {
        [HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
        public static void ResolvedAllowedDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            AddDesignators(__instance, ref __result);
        }

        [HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
        public static void AllResolvedAndIdeoDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            AddDesignators(__instance, ref __result);
        }

        private static void AddDesignators(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            HashSet<Designator> geneDesignators = new();

            foreach (var defExtension_designator in Faction.OfPlayer.GetPawns().SelectMany(pawn => pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()))
            {
                geneDesignators.AddRange(defExtension_designator.addDesignators.Where(def => def.designationCategory == __instance).Select(GetCachedDesignator));
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
