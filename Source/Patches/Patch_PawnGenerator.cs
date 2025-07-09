using System.Collections.Generic;
using System.Reflection;
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
                        // Load 'request'
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
            if (!Fixup_TryGenerateNewPawnInternal.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(
                    $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}: {reason}");
            return instructionsList;
        }

        public static void ModifyGenderByGenes(Pawn pawn, ref PawnGenerationRequest request, XenotypeDef xenotype)
        {
            using (new ProfileBlock())
            {
                var gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                           request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                           request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ??
                           xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
                if (gene != null)
                {
                    pawn.gender = Rand.Chance(gene.GetModExtension<GeneDefExtension_GenderRatio>().femaleChance)
                        ? Gender.Female
                        : Gender.Male;
                }
            }
        }

        public static bool HasGenderRatio(GeneDef gene)
        {
            return gene.GetModExtension<GeneDefExtension_GenderRatio>() != null;
        }
    }
}
