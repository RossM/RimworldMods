using HarmonyLib;
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
        public static bool NeedJoySource_Prefix(Map map, out bool __result)
        {
            __result = false;

            return map.mapPawns.FreeColonists.Any(pawn => pawn.needs.joy != null);
        }
    }
}
