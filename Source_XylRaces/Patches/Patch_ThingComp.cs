namespace XylXenos.Patches;

[HarmonyPatch(typeof(ThingComp))]
public static class Patch_ThingComp
{
    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ThingComp.Initialize))]
    public static void Initialize_Postfix(ThingComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is INotificationListener target)
            target.RegisterWith(NotificationManager.Instance);
    }

    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ThingComp.PostDestroy))]
    public static void PostDestroy_Postfix(ThingComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is INotificationListener target)
            NotificationManager.Instance.UnregisterAll(target);
    }
}
