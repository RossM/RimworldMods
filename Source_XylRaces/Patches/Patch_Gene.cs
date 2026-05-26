using HarmonyLib;
using Verse;
using XylXenos.Genes;

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

        [Feature(nameof(DefExt.hediffGivers))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.TickInterval))]
        public static void TickInterval_Postfix(Gene __instance, int delta)
        {
            __instance.TickIntervalExt(delta);
        }
    }
}
