using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Alert_NeedJoySources))]
    public static class Patch_Alert_NeedJoySources
    {
        [Feature("Joyless"), HarmonyPrefix, UsedImplicitly, HarmonyPatch("NeedJoySource")]
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
