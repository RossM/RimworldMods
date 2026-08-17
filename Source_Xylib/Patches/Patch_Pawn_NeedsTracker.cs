namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_NeedsTracker))]
internal static class Patch_Pawn_NeedsTracker
{
    [Feature(typeof(EventManager))]
    [Postfix] [Inner(typeof(Need), nameof(Need.OnNeedRemoved))]
    [Target("RemoveNeed")]
    public static void OnNeedRemoved_Postfix(Need __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }

    [Feature(typeof(EventManager))]
    [Postfix] [Inner(typeof(Need), nameof(Need.SetInitialLevel))]
    [Target("AddNeed")]
    public static void SetInitialLevel_Postfix(Need __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.AddListener(target);
    }
}
