namespace Xylib.Patches;

[HarmonyPatch(typeof(HediffCompProperties))]
internal static class Patch_HediffCompProperties
{
    [Feature(nameof(PatchHelpers.RequiredMemberErrors))]
    [Postfix]
    [Target(nameof(HediffCompProperties.ConfigErrors))]
    public static void ConfigErrors_Postfix(object __instance, ref IEnumerable<string> __result)
    {
        var extraErrors = PatchHelpers.RequiredMemberErrors(__instance);

        if (extraErrors is { Count: > 0 })
            __result = __result.Concat(extraErrors);
    }
}
