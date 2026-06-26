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
            EventManager.Instance.AddListener(target);
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MapComponent.MapRemoved))]
    public static void MapRemoved_Postfix(MapComponent __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }
}
