using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using TranspilerUtil;
using UnityEngine;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(ConversionUtility))]
    public static class Patch_ConversionUtility
    {
        [Feature(nameof(XenotypeDefExtension.agreeingMemes))]
        [Feature(nameof(XenotypeDefExtension.disagreeingMemes))]
        [InfixPostfix(typeof(ConversionUtility), "<ConversionPowerFactor_MemesVsTraits>g__OffsetFromIdeo|1_1")]
        [InfixPatch(typeof(ConversionUtility), nameof(ConversionUtility.ConversionPowerFactor_MemesVsTraits))]
        public static void OffsetFromIdeo_Postfix(Pawn pawn, bool invert, StringBuilder sb, Pawn recipient, ref float __result)
        {
            __result += PatchHelpers.ConversionPowerFactor_OffsetFromXenotype(pawn, invert, sb, recipient);
        }
    }
}
