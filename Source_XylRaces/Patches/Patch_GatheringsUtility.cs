using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GatheringsUtility))]
    public static class Patch_GatheringsUtility
    {
        [Feature(Config.Feature.Joyless)]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GatheringsUtility.ShouldPawnKeepGathering))]
        public static bool ShouldPawnKeepGathering_Prefix(Pawn p, GatheringDef gatheringDef, ref bool __result)
        {
            __result = false;
            if (gatheringDef.respectTimetable && p.needs.joy == null)
                return false;
            return true;
        }
    }
}
