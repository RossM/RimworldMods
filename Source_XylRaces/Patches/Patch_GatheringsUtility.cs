using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GatheringsUtility))]
    public static class Patch_GatheringsUtility
    {
        [Feature("Joyless")]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(GatheringsUtility.ShouldPawnKeepGathering))]
        public static bool ShouldPawnKeepGathering(Pawn p, GatheringDef gatheringDef, ref bool __result)
        {
            __result = false;
            if (gatheringDef.respectTimetable && p.needs.joy == null)
                return false;
            return true;
        }
    }
}
