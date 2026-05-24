using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(XenotypeSet))]
    public static class Patch_XenotypeSet
    {
        [Feature(typeof(XenotypeSetWithDefault))]
        [InfixWrapper(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(XenotypeSet.BaselinerChance))]
        [InfixPatch(nameof(XenotypeSet.Contains))]
        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(XenotypeSet __caller)
        {
            return __caller.GetDefaultXenotype();
        }
    }
}
