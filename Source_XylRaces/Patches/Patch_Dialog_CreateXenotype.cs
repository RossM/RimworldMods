using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Dialog_CreateXenotype))]
    public class Patch_Dialog_CreateXenotype
    {
        [Feature(typeof(GeneDefExtension_UIFilter))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder))]
        [InfixPatch("DrawGenes")]
        public static List<GeneDef> GenesInOrder_Wrapper(Dialog_CreateXenotype __caller)
        {
            var result = GeneUtility.GenesInOrder;
            return FilterGenes(result, __caller.inheritable, __caller.ignoreRestrictions);
        }

        private static List<GeneDef> FilterGenes(List<GeneDef> genes, bool inheritable, bool ignoreRestrictions)
        {
            if (ignoreRestrictions)
                return genes;
            return genes.Where(g => GeneHelpers.GeneShouldBeVisible(g, inheritable)).ToList();
        }
    }
}
