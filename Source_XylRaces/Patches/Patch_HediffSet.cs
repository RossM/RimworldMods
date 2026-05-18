using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HediffSet))]
    public class Patch_HediffSet
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(HediffSet.DirtyCache))]
        public static void DirtyCache_Postfix(HediffSet __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.HediffsChanged, __instance.pawn);
        }
    }
}
