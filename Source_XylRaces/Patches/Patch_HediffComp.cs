using HarmonyLib;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HediffComp))]
    public static class Patch_HediffComp
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(HediffComp.CompPostPostAdd))]
        public static void CompPostPostAdd_Postfix(HediffComp __instance)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (__instance is INotificationTarget target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
