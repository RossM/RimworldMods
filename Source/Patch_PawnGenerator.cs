using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;


namespace XylRacesCore
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
        static IEnumerable<CodeInstruction> TryGenerateNewPawnInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_TryGenerateNewPawnInternal.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }

        static void ModifyGenderByGenes(Pawn pawn, ref PawnGenerationRequest request, XenotypeDef xenotype)
        {
            //Log.Message(string.Format("ModifyGenderByGenes: pawn = {0}, request = ({1}), xenotype = {2}", pawn, request, xenotype));
            //Log.Message(string.Format("  ForcedCustomXenotype = {0}", request.ForcedCustomXenotype));
            var gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ?? 
                       xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
            if (gene != null)
            {
                pawn.gender = Rand.Chance(gene.GetModExtension<GeneDefExtension_GenderRatio>().femaleChance)
                    ? Gender.Female : Gender.Male;
            }
        }

        static bool HasGenderRatio(GeneDef gene)
        {
            return gene.GetModExtension<GeneDefExtension_GenderRatio>() != null;
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(PawnGenerator.GetXenotypeForGeneratedPawn))]
        static void GetXenotypeForGeneratedPawn_Postfix(PawnGenerationRequest request, ref XenotypeDef __result)
        {
            if (Find.Scenario != null)
            {
                foreach (ScenPart allPart in Find.Scenario.AllParts)
                {
                    var part = allPart as ScenPart_ForcedXenotype;
                    if (part == null)
                        continue;

                    part.ModifyXenotype(request.Faction, request.Context, ref __result);
                }
            }
        }
    }
}
