using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GenConstruct))]
    public static class Patch_GenConstruct
    {
        private static readonly InstructionMatcher.Rule Rule_MembersCanBuild
            = InstructionMatcher.MakeRedirectRule(nameof(Ideo.MembersCanBuild), MembersCanBuild_Wrapper);

        [Feature(typeof(GeneDefExtension_Designator))]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(GenConstruct.CanConstruct), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef)])]
        public static IEnumerable<CodeInstruction> CanConstruct_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_MembersCanBuild
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static bool MembersCanBuild_Wrapper(Ideo __instance, Thing thing, Pawn p)
        {
            if (__instance.MembersCanBuild(thing))
                return true;

            if (__instance != p.Ideo)
                return false;

            BuildableDef def = thing.def.entityDefToBuild ?? thing.def;

            bool hasGeneDesignator = p.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()
                .Any(defExtension_designator => defExtension_designator.addDesignators.Contains(def));
            if (!hasGeneDesignator && GenConstruct.tmpIdeoMemberNames.Count == 0)
            {
                foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs)
                {
                    if (gene.GetModExtension<GeneDefExtension_Designator>()?.addDesignators.Contains(def) ?? false)
                        GenConstruct.tmpIdeoMemberNames.Add("XylCharactersWithGene".Translate(gene.LabelCap));
                }
            }

            return hasGeneDesignator;
        }
    }
}
