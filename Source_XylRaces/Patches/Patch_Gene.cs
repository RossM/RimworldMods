using HarmonyLib;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Gene))]
    public static class Patch_Gene
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.PostAdd))]
        public static void PostAdd_Postfix(Gene __instance)
        {
            if (__instance is INotificationListener target)
                target.RegisterWith(NotificationManager.Instance);
        }

        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.PostRemove))]
        public static void PostRemove_Postfix(Gene __instance)
        {
            if (__instance is INotificationListener target)
                NotificationManager.Instance.UnregisterAll(target);
        }
    }
}
