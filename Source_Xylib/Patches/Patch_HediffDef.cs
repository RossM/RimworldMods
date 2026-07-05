namespace Xylib.Patches;

[HarmonyPatch(typeof(HediffDef))]
internal static class Patch_HediffDef
{
    private static readonly List<StatDrawEntry> statDrawEntries = [];

    [Feature(typeof(HediffCompPropertiesExt))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HediffDef.SpecialDisplayStats))]
    public static void SpecialDisplayStats_Postfix(HediffDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        if (__instance.comps.NullOrEmpty())
            return;

        statDrawEntries.Clear();
        foreach (var props in __instance.comps.OfType<HediffCompPropertiesExt>())
            statDrawEntries.AddRange(props.SpecialDisplayStats(req));

        if (statDrawEntries.Count > 0)
            __result = __result.Concat(statDrawEntries);
    }
}
