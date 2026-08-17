namespace XylXenos.Patches;

[HarmonyPatch(typeof(FactionDef))]
public static class Patch_FactionDef
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [Postfix] [Inner(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [Target(nameof(FactionDef.Description))]
    public static void XenotypeDefOf_Baseliner_Postfix(FactionDef __caller, ref XenotypeDef? __result)
    {
        if (__caller.xenotypeSet is XenotypeSetWithDefault withDefault)
            __result = withDefault.defaultXenotype;
    }
}
