using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
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
            if (ignoreRestrictions)
                return genes;
            return genes.Where(g => g.GetModExtension<GeneDefExtension_UIFilter>()?.ShouldBeVisible(inheritable) != false).ToList();
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DrawGenes")]
        public static IEnumerable<CodeInstruction> DrawGenes_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_DrawGenes.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}.{1}: {2}", MethodBase.GetCurrentMethod()?.DeclaringType?.Name, MethodBase.GetCurrentMethod()?.Name, reason));
            return instructionsList;
        }

    }
}
