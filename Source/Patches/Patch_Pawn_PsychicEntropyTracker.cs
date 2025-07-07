using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_PsychicEntropyTracker))]
    public static class Patch_Pawn_PsychicEntropyTracker
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedsPsyfocus), MethodType.Getter)]
        public static bool NeedsPsyfocus_Prefix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
        {
            using (new ProfileBlock())
            {
                __result = __instance.Pawn.NeedsPsyfocus();
                return false;
            }
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedToShowGizmo))]
        public static bool NeedToShowGizmo_Prefix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
        {
            using (new ProfileBlock())
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
}
