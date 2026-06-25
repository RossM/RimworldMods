namespace XylXenos.Patches;

[HarmonyPatch(typeof(GeneDef))]
public static class Patch_GeneDef
{
    [Feature(typeof(DefModExtension_GeneWithComps))]
    [InfixPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
    [InfixPatch("GetDescriptionFull")]
    public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
    {
        var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
        if (extraDescriptions.Count == 0)
            return;

        __result = __result.NullOrEmpty() ? extraDescriptions : __result.Concat(extraDescriptions).ToList();
    }

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
