using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Dialog_CreateXenotype))]
    public class Patch_Dialog_CreateXenotype
    {
        private static readonly InstructionMatcher Fixup_DrawGenes = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.PropertyGetter(typeof(GeneUtility), nameof(GeneUtility.GenesInOrder)),
                    AccessTools.Method(typeof(Patch_Dialog_CreateXenotype), nameof(GenesInOrder_Wrapper))
                    )
            }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<GeneDef> GenesInOrder_Wrapper(Dialog_CreateXenotype __caller)
        {
            var result = GeneUtility.GenesInOrder;
            return FilterGenes(result, __caller.inheritable, __caller.ignoreRestrictions);
        }

        private static List<GeneDef> FilterGenes(List<GeneDef> genes, bool inheritable, bool ignoreRestrictions)
        {
            using (new ProfileBlock())
            {
                if (ignoreRestrictions)
                    return genes;
                return genes.Where(g =>
                    g.GetModExtension<GeneDefExtension_UIFilter>()?.ShouldBeVisible(inheritable) != false).ToList();
            }
        }

        [Feature(nameof(GeneDefExtension_UIFilter)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DrawGenes")]
        public static IEnumerable<CodeInstruction> DrawGenes_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DrawGenes.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

    }
}
