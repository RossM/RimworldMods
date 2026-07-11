namespace Xylib.Patches;

[HarmonyPatch(typeof(Def))]
internal static class Patch_Def
{
    [Feature(nameof(PatchHelpers.RequiredMemberErrors))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Def.ConfigErrors))]
    public static void ConfigErrors_Postfix(object __instance, ref IEnumerable<string> __result)
    {
        var extraErrors = PatchHelpers.RequiredMemberErrors(__instance);

        if (extraErrors is { Count: > 0 })
            __result = __result.Concat(extraErrors);
    }
}
