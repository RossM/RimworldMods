namespace XylXenos.Patches;

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

    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MapComponent.MapRemoved))]
    public static void MapRemoved_Postfix(MapComponent __instance)
    {
        if (__instance is INotificationListener target)
            NotificationManager.Instance.UnregisterAll(target);
    }
}