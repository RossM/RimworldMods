namespace Xylib.Patches;

[HarmonyPatch(typeof(DefModExtension))]
internal static class Patch_DefModExtension
{
    [Feature(nameof(PatchHelpers.RequiredMemberErrors))]
    [Postfix]
    [Target(nameof(DefModExtension.ConfigErrors))]
    public static void ConfigErrors_Postfix(object __instance, ref IEnumerable<string> __result)
    {
        var extraErrors = PatchHelpers.RequiredMemberErrors(__instance);

        if (extraErrors is { Count: > 0 })
            __result = __result.Concat(extraErrors);
    }
}
