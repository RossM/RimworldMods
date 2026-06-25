namespace Xylib.Patches;

[HarmonyPatch(typeof(HediffComp))]
public static class Patch_HediffComp
{
    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HediffComp.CompPostPostAdd))]
    public static void CompPostPostAdd_Postfix(HediffComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            target.RegisterWith(EventManager.Instance);
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HediffComp.CompPostPostRemoved))]
    public static void CompPostPostRemoved_Postfix(HediffComp __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.UnregisterAll(target);
    }
}
