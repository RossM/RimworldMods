namespace XylXenos.Patches;

[HarmonyPatch(typeof(Hediff))]
public static class Patch_Hediff
{
    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff.PostAdd))]
    public static void PostAdd_Postfix(Hediff __instance)
    {
        if (__instance is INotificationListener target)
            target.RegisterWith(NotificationManager.Instance);
    }

    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff.PostRemoved))]
    public static void PostRemoved_Postfix(Hediff __instance)
    {
        if (__instance is INotificationListener target)
            NotificationManager.Instance.UnregisterAll(target);
    }
}