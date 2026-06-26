namespace Xylib.Patches;

[HarmonyPatch(typeof(Gene))]
public static class Patch_Gene
{
    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Gene.PostAdd))]
    public static void PostAdd_Postfix(Gene __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.AddListener(target);
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Gene.PostRemove))]
    public static void PostRemove_Postfix(Gene __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }
}
