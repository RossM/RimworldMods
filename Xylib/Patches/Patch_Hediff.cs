namespace Xylib.Patches;

[HarmonyPatch(typeof(Hediff))]
public static class Patch_Hediff
{
    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff.PostAdd))]
    public static void PostAdd_Postfix(Hediff __instance)
    {
        if (__instance is IEventListener target)
            target.RegisterWith(EventManager.Instance);
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff.PostRemoved))]
    public static void PostRemoved_Postfix(Hediff __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.UnregisterAll(target);
    }
}
