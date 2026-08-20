namespace XylXenos.Patches;

[HarmonyPatch(typeof(XenotypeSet))]
public static class Patch_XenotypeSet
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [Postfix]
    [Inner(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [Target(nameof(XenotypeSet.BaselinerChance))]
    [Target(nameof(XenotypeSet.Contains))]
    public static void XenotypeDefOf_Baseliner_Postfix(XenotypeSet __caller, ref XenotypeDef? __result)
    {
        if (__caller is XenotypeSetWithDefault withDefault)
            __result = withDefault.defaultXenotype;
    }
}
