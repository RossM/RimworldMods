namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_NeedsTracker))]
public static class Patch_Pawn_NeedsTracker
{
    [Feature(typeof(EventManager))]
    [InfixPostfix(typeof(Need), nameof(Need.OnNeedRemoved))]
    [InfixPatch("RemoveNeed")]
    public static void OnNeedRemoved_Postfix(Need __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener listener)
            EventManager.Instance.UnregisterAll(listener);
    }

    [Feature(typeof(EventManager))]
    [InfixPostfix(typeof(Need), nameof(Need.SetInitialLevel))]
    [InfixPatch("AddNeed")]
    public static void SetInitialLevel_Postfix(Need __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener listener)
            listener.RegisterWith(EventManager.Instance);
    }
}
