namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneDef))]
public static class Patch_GeneDef
{
    [Feature(typeof(DefModExtension_GeneWithComps))]
    [HarmonyPostfix]
    [HarmonyPatch("SpecialDisplayStats")]
    public static void GeneDef_SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        var defExt = __instance.GetModExtension<DefModExtension_GeneWithComps>();
        if (defExt == null)
            return;

        __result = __result.Concat(defExt.SpecialDisplayStats(req));
    }
}