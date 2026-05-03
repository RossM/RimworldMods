using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    public static class Patch_PawnGenerator
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] public static GeneDef XylEcholocation;
        }

        private static readonly InstructionMatcher Fixup_TryGenerateNewPawnInternal = new()
        {
            Rules =
            {
                InstructionMatcher.RedirectMethodRule(
                    AccessTools.Method(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo)),
                    AccessTools.Method(typeof(Patch_PawnGenerator), nameof(GiveAppropriateBioAndNameTo_Wrapper))
                    )
            }
        };

        [Feature(nameof(GeneDefExtension_GenderRatio)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("TryGenerateNewPawnInternal")]
        public static IEnumerable<CodeInstruction> TryGenerateNewPawnInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_TryGenerateNewPawnInternal.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static void GiveAppropriateBioAndNameTo_Wrapper(Pawn pawn, FactionDef factionType, PawnGenerationRequest request,
            XenotypeDef xenotype)
        {
            ModifyGenderByGenes(pawn, request, xenotype);
            PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, factionType, request, xenotype);
        }

        public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef xenotype)
        {
            using (new ProfileBlock())
            {
                if (request.FixedGender != null)
                    return;

                GeneDef gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                               request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                               request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ??
                               xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
                if (gene == null) 
                    return;

                pawn.gender = gene.GetModExtension<GeneDefExtension_GenderRatio>().GetGender();
            }
        }

        public static bool HasGenderRatio(GeneDef gene)
        {
            return gene.GetModExtension<GeneDefExtension_GenderRatio>() != null;
        }

        [Feature(nameof(GeneDefExtension_CongenitalHediff)), HarmonyPostfix, UsedImplicitly, HarmonyPatch("GenerateInitialHediffs")]
        public static void GenerateInitialHediffs_Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            using (new ProfileBlock())
            {
                foreach (var extension in pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_CongenitalHediff>())
                {
                    if (!Rand.Chance(extension.chance))
                        continue;

                    foreach (var hediffGiver in extension.hediffGivers)
                        hediffGiver.TryApply(pawn);
                }
            }
        }
    }
}
