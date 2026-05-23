using HarmonyLib;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(MapComponent))]
    public static class Patch_MapComponent
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MapComponent.FinalizeInit))]
        public static void FinalizeInit_Postfix(MapComponent __instance)
        {
            if (__instance is INotificationListener target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
