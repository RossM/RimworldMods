using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FactionDef))]
    public static class Patch_FactionDef
    {
        [Feature(typeof(XenotypeSetWithDefault))]
        [InfixPostfix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(FactionDef.Description))]
        public static void XenotypeDefOf_Baseliner_Postfix(FactionDef __caller, ref XenotypeDef __result)
        {
            __result = __caller.xenotypeSet.DefaultXenotype;
        }
    }
}
