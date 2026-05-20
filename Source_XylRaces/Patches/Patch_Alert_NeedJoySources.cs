using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Alert_NeedJoySources))]
    public static class Patch_Alert_NeedJoySources
    {
        [Feature(Config.Feature.Joyless)]
        [HarmonyPrefix]
        [HarmonyPatch("NeedJoySource")]
        public static bool NeedJoySource_Prefix(Map map, bool __result)
        {
            __result = false;

            // Check if any pawns need joy
            if (!map.mapPawns.FreeColonists.Any(pawn => pawn.needs.joy != null))
                return false;

            // Continue to regular checks
            return true;
        }
    }
}
