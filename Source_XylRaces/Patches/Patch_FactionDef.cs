using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FactionDef))]
    public static class Patch_FactionDef
    {
        [Feature(typeof(XenotypeSetWithDefault))]
        [InfixPrefix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(FactionDef.Description))]
        public static bool XenotypeDefOf_Baseliner_Prefix(FactionDef __caller, out XenotypeDef __result)
        {
            __result = __caller.xenotypeSet.GetDefaultXenotype();
            return false;
        }
    }
}
