using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TranspilerUtil;
using Verse;
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
                InstructionMatcher.RedirectMethodRule(
                    AccessTools.Method(typeof(Ideo), nameof(Ideo.MembersCanBuild)),
                    AccessTools.Method(typeof(Patch_GenConstruct), nameof(MembersCanBuild_Wrapper))
                    )
            }
        };

        [Feature(nameof(GeneDefExtension_Designator)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(GenConstruct.CanConstruct), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef)])]
        public static IEnumerable<CodeInstruction> CanConstruct_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_CanConstruct.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static bool MembersCanBuild_Wrapper(Ideo __instance, Thing thing, Pawn p)
        {
            using (new ProfileBlock())
            {
                if (p.Ideo.MembersCanBuild(thing))
                    return true;

                BuildableDef def = thing.def.entityDefToBuild ?? thing.def;

                var result = p.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()
                    .Any(defExtension_designator => defExtension_designator.addDesignators.Contains(def));
                if (!result)
                {
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
