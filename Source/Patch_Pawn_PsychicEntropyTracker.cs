using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Pawn_PsychicEntropyTracker))]
    public static class Patch_Pawn_PsychicEntropyTracker
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedsPsyfocus), MethodType.Getter)]
        public static bool NeedsPsyfocus_Prefix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
        {
            __result = __instance.Pawn.NeedsPsyfocus();
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedToShowGizmo))]
        public static bool NeedToShowGizmo_Prefix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
        {
            if (__instance.Pawn.HasPsycastGene())
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
