using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    public static class Patch_PawnGenerator
    {
        private static readonly InstructionMatcher Fixup_TryGenerateNewPawnInternal = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 1,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        // PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, faction.def, request, xenotype);
                        // At this point 'pawn' is on the top of the stack
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.LoadField(typeof(Faction), "def"),
                        CodeInstruction.LoadArgument(0),
                        new CodeInstruction(OpCodes.Ldobj, typeof(PawnGenerationRequest)),
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        CodeInstruction.Call(typeof(PawnBioAndNameGenerator), "GiveAppropriateBioAndNameTo"), 
                    ],
                    Output =
                    [
                        // Duplicate 'pawn'
                        new CodeInstruction(OpCodes.Dup),
                        // Load 'ref request'
                        CodeInstruction.LoadArgument(0), 
                        // Load 'xenotype'
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        // Call our fixup
                        CodeInstruction.Call(() => ModifyGenderByGenes),

                    ],
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("TryGenerateNewPawnInternal")]
        public static IEnumerable<CodeInstruction> TryGenerateNewPawnInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_TryGenerateNewPawnInternal.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static void ModifyGenderByGenes(Pawn pawn, ref PawnGenerationRequest request, XenotypeDef xenotype)
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
    }
}
