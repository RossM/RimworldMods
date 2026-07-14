namespace XylXenos.Patches;

[HarmonyPatch(typeof(XenotypeSet))]
public static class Patch_XenotypeSet
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [InnerPostfix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [Target(nameof(XenotypeSet.BaselinerChance))]
    [Target(nameof(XenotypeSet.Contains))]
    public static void XenotypeDefOf_Baseliner_Postfix(XenotypeSet __caller, ref XenotypeDef? __result)
    {
        __result = __caller.DefaultXenotype;
    }
}
