namespace Xylib.Patches;

[HarmonyPatch(typeof(AbilityCompProperties))]
internal static class Patch_AbilityCompProperties
{
    [Feature(nameof(PatchHelpers.RequiredMemberErrors))]
    [Postfix]
    [Target(nameof(AbilityCompProperties.ConfigErrors))]
    public static void ConfigErrors_Postfix(object __instance, ref IEnumerable<string> __result)
    {
        var extraErrors = PatchHelpers.RequiredMemberErrors(__instance);

        if (extraErrors is { Count: > 0 })
            __result = __result.Concat(extraErrors);
    }
}
