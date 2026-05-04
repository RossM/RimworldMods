using HarmonyLib;
using RimWorld;
using JetBrains.Annotations;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
    public static class Patch_JobGiver_OptimizeApparel
    {
        [Feature(nameof(Flight)), HarmonyPrefix, UsedImplicitly,
         HarmonyPatch(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
        public static bool ApparelScoreRaw_Prefix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (pawn.HasActiveGeneOfType<Flight>() && !Flight.ApparelAllowsFlight(ap.def))
            {
                __result = -10f;
                return false;
            }

            return true;
        }
    }
}
