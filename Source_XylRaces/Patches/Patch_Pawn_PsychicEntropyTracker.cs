using HarmonyLib;
using RimWorld;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_PsychicEntropyTracker))]
    public static class Patch_Pawn_PsychicEntropyTracker
    {
        // Note: This patch is performance-sensitive
        [Feature(typeof(Psycast))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedsPsyfocus), MethodType.Getter)]
        public static bool NeedsPsyfocus_Prefix(Pawn_PsychicEntropyTracker __instance, out bool __result)
        {
            __result = __instance.Pawn.NeedsPsyfocus();
            return false;
        }

        [Feature(typeof(Psycast))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn_PsychicEntropyTracker.NeedToShowGizmo))]
        public static void NeedToShowGizmo_Postfix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
        {
            if (__instance.Pawn.HasActivePsycastGene())
                __result = true;
        }
    }
}
