namespace Xylib.Patches;

[HarmonyPatch(typeof(MapComponent))]
internal static class Patch_MapComponent
{
    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(MapComponent.FinalizeInit))]
    public static void FinalizeInit_Postfix(MapComponent __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.AddListener(target);
    }

    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(MapComponent.MapRemoved))]
    public static void MapRemoved_Postfix(MapComponent __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }
}
