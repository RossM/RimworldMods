namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneDef))]
internal static class Patch_GeneDef
{
    [Feature(typeof(DefModExtension_GeneWithComps))]
    [InfixPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
    [InfixPatch("GetDescriptionFull")]
    public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
    {
        var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
        if (extraDescriptions.Count == 0)
            return;

        __result = __result is not { Count: > 0 } ? extraDescriptions : [.. __result, .. extraDescriptions];
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [HarmonyPostfix]
    [HarmonyPatch("SpecialDisplayStats")]
    public static void GeneDef_SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        var defExt = __instance.Extension_GeneWithComps;
        if (defExt == null)
            return;

        __result = __result.Concat(defExt.SpecialDisplayStats(req));
    }
}
