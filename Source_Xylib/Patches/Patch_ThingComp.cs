namespace Xylib.Patches;

[HarmonyPatch(typeof(ThingComp))]
internal static class Patch_ThingComp
{
    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(ThingComp.Initialize))]
    public static void Initialize_Postfix(ThingComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.AddListener(target);
    }

    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(ThingComp.PostDestroy))]
    public static void PostDestroy_Postfix(ThingComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }
}
