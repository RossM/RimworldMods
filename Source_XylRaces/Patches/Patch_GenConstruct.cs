using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(GenConstruct))]
    public static class Patch_GenConstruct
    {
        private static readonly InstructionMatcher Fixup_CanConstruct = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 1,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.Call(typeof(Pawn), "get_" + nameof(Pawn.Ideo)),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(typeof(Ideo), nameof(Ideo.MembersCanBuild)),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.Call(() => CanBuildHelper),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(GenConstruct.CanConstruct), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef)])]
        public static IEnumerable<CodeInstruction> CanConstruct_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_CanConstruct.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static bool CanBuildHelper(Thing thing, Pawn pawn)
        {
            using (new ProfileBlock())
            {
                if (pawn.Ideo.MembersCanBuild(thing))
                    return true;

                BuildableDef def = thing.def.entityDefToBuild ?? thing.def;

                var result = pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()
                    .Any(defExtension_designator => defExtension_designator.addDesignators.Contains(def));
                if (!result)
                {
                    var geneNames = new List<string>();
                    foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs)
                    {
                        if (gene.GetModExtension<GeneDefExtension_Designator>()?.addDesignators.Contains(def) ?? false)
                            GenConstruct.tmpIdeoMemberNames.Add("XylCharactersWithGene".Translate(gene.LabelCap));
                    }
                }

                return result;
            }
        }
    }
}
