namespace Xylib.Patches;

[HarmonyPatch(typeof(MapComponent))]
public static class Patch_MapComponent
{
    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MapComponent.FinalizeInit))]
    public static void FinalizeInit_Postfix(MapComponent __instance)
    {
        if (__instance is IEventListener target)
            target.RegisterWith(EventManager.Instance);
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MapComponent.MapRemoved))]
    public static void MapRemoved_Postfix(MapComponent __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.UnregisterAll(target);
    }
}
