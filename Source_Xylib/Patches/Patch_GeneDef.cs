namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneDef))]
internal static class Patch_GeneDef
{
    // GeneDef.ConfigErrors doesn't call Def.ConfigErrors, resulting in DefModExtension.ConfigErrors not getting called
    // for gene mod extensions. This breaks GeneWithComps config error reporting.
    [Feature("BUGFIX")]
    [Postfix]
    [Target(nameof(GeneDef.ConfigErrors))]
    public static void ConfigErrors_Postfix(ref IEnumerable<string> __result, Func<IEnumerable<string>> __base)
    {
        __result = __result.Concat(__base());
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [InnerPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
    [Target("GetDescriptionFull")]
    public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
    {
        var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
        if (extraDescriptions.Count == 0)
            return;

        __result = __result is not { Count: > 0 } ? extraDescriptions : [.. __result, .. extraDescriptions];
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [Postfix]
    [Target("SpecialDisplayStats")]
    public static void GeneDef_SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        var defExt = __instance.Extension_GeneWithComps;
        if (defExt == null)
            return;

        __result = __result.Concat(defExt.SpecialDisplayStats(req));
    }
}
