using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker))]
    public static class Patch_Pawn_ApparelTracker
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
        public static void Notify_ApparelChanged_Postfix(Pawn_ApparelTracker __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.ApparelChanged, __instance.pawn);
        }
    }
}
