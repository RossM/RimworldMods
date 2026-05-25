using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_HealthTracker))]
    public static class Patch_Pawn_HealthTracker
    {
        [Feature(nameof(DefExt.permanentHediffs))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn_HealthTracker.CheckForStateChange))]
        public static void CheckForStateChange_Postfix(Pawn_HealthTracker __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PostHediffStateChange, __instance.pawn);
        }
    }
}
