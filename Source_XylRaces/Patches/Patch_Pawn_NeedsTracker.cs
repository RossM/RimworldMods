namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_NeedsTracker))]
public static class Patch_Pawn_NeedsTracker
{
    [Feature(typeof(NotificationManager))]
    [InfixPostfix(typeof(Need), nameof(Need.SetInitialLevel))]
    [InfixPatch("AddNeed")]
    public static void SetInitialLevel_Postfix(Need __instance)
    {
        if (__instance is INotificationListener listener)
            listener.RegisterWith(NotificationManager.Instance);
    }

    [Feature(typeof(NotificationManager))]
    [InfixPostfix(typeof(Need), nameof(Need.OnNeedRemoved))]
    [InfixPatch("RemoveNeed")]
    public static void OnNeedRemoved_Postfix(Need __instance)
    {
        if (__instance is INotificationListener listener)
            NotificationManager.Instance.UnregisterAll(listener);
    }

}
