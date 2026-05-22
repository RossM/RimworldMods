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
        private static readonly InstructionMatcher.Rule Rule_GenesInOrder = InstructionMatcher.MakeRedirectRule(
            AccessTools.PropertyGetter(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder)),
            GenesInOrder_Wrapper
        );

        [Feature(typeof(GeneDefExtension_UIFilter))]
        [HarmonyTranspiler]
        [HarmonyPatch("DrawGenes")]
        public static IEnumerable<CodeInstruction> DrawGenes_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_GenesInOrder
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
