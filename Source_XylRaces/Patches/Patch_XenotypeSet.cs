using HarmonyLib;
using RimWorld;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(XenotypeSet))]
    public static class Patch_XenotypeSet
    {
        [Feature(typeof(XenotypeSetWithDefault))]
        [WrappedMember(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(XenotypeSet.BaselinerChance))]
        [InfixPatch(nameof(XenotypeSet.Contains))]
        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(XenotypeSet __caller)
        {
            return XenotypeSetWithDefault.GetDefaultXenotype(__caller);
        }
    }
}
