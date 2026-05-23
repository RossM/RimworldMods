using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FactionDef))]
    public static class Patch_FactionDef
    {
        [Feature(typeof(XenotypeSetWithDefault))]
        [InfixWrapper(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(FactionDef.Description))]
        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(FactionDef __caller)
        {
            return XenotypeSetWithDefault.GetDefaultXenotype(__caller.xenotypeSet);
        }
    }
}
