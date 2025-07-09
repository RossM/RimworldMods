using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertAfter,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(GeneUtility), "get_" + nameof(GeneUtility.GenesInOrder)),
                    ],
                    Output =
                    [
                        // Load this
                        CodeInstruction.LoadArgument(0),
                        // Load this.inheritable
                        CodeInstruction.LoadField(typeof(Dialog_CreateXenotype), "inheritable"),
                        // Load this
                        CodeInstruction.LoadArgument(0),
                        // Load this.inheritable
                        CodeInstruction.LoadField(typeof(Dialog_CreateXenotype), "ignoreRestrictions"),
                        // Call
                        CodeInstruction.Call(() => FilterGenes),
                    ]
                }
            }
        };

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

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DrawGenes")]
        public static IEnumerable<CodeInstruction> DrawGenes_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DrawGenes.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

    }
}
