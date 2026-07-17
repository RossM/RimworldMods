namespace Xylib.Patches;

[HarmonyPatch(typeof(Hediff))]
internal static class Patch_Hediff
{
    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(Hediff.PostAdd))]
    public static void PostAdd_Postfix(Hediff __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.AddListener(target);
    }

    [Feature(typeof(EventManager))]
    [Postfix]
    [Target(nameof(Hediff.PostRemoved))]
    public static void PostRemoved_Postfix(Hediff __instance)
    {
        if (__instance is IEventListener target)
            EventManager.Instance.RemoveListener(target);
    }
}
