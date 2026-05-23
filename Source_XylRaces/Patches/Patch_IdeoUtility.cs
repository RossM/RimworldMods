using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(IdeoUtility))]
    public static class Patch_IdeoUtility
    {
        [Feature(typeof(IncidentWorker_WildTribe))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(IdeoUtility.CanUseIdeo))]
        public static bool CanUseIdeo_Prefix(FactionDef factionDef, Ideo ideo, IdeoGenerationParms parms, ref bool __result)
        {
            __result = false;

            XenotypeSet xenotypeSet = parms.forFaction?.xenotypeSet;
            if (xenotypeSet == null)
                return true;

            var precept = (Precept_Xenotype)ideo.GetPrecept(PreceptDefOf.PreferredXenotype);
            if (precept == null)
                return true;

            return xenotypeSet.Contains(precept.xenotype);
        }

    }
}
