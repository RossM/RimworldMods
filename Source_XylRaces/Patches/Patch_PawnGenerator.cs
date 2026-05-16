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
        private static readonly InstructionMatcher Fixup_TryGenerateNewPawnInternal = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo, GiveAppropriateBioAndNameTo_Wrapper)
            }
        };

        private static readonly InstructionMatcher Fixup_DefaultXenotype = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(XenotypeDefOf.Baseliner), XenotypeDefOf_Baseliner_Wrapper),
                InstructionMatcher.MakeRedirectRule("<XenotypesAvailableFor>g__AddOrAdjust|49_0", AddOrAdjust_Wrapper),
            }
        };

        [Feature(nameof(GeneDefExtension_GenderRatio))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch("TryGenerateNewPawnInternal")]
        public static IEnumerable<CodeInstruction> TryGenerateNewPawnInternal_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_TryGenerateNewPawnInternal.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static void GiveAppropriateBioAndNameTo_Wrapper(
            Pawn pawn,
            FactionDef factionType,
            PawnGenerationRequest request,
            XenotypeDef xenotype)
        {
            ModifyGenderByGenes(pawn, request, xenotype);
            PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, factionType, request, xenotype);
        }

        public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef xenotype)
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

        public static bool HasGenderRatio(GeneDef gene)
        {
            return gene.GetModExtension<GeneDefExtension_GenderRatio>() != null;
        }

        [Feature(nameof(GeneDefExtension_CongenitalHediff))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch("GenerateInitialHediffs")]
        public static void GenerateInitialHediffs_Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            foreach (var extension in pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_CongenitalHediff>())
            {
                if (!Rand.Chance(extension.chance))
                    continue;

                foreach (var hediffGiver in extension.hediffGivers)
                    hediffGiver.TryApply(pawn);
            }
        }

        [Feature(nameof(XenotypeSetWithDefault))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
        public static IEnumerable<CodeInstruction> XenotypesAvailableFor_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DefaultXenotype.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(FactionDef factionDef = null, Faction faction = null)
        {
            FactionDef factionDef2 = faction?.def ?? factionDef;
            return XenotypeSetWithDefault.GetDefaultXenotype(factionDef2?.xenotypeSet);
        }

        public static void AddOrAdjust_Wrapper(XenotypeChance xenotypeChance, FactionDef factionDef = null, Faction faction = null)
        {
            FactionDef factionDef2 = faction?.def ?? factionDef;
            if (xenotypeChance.xenotype != XenotypeSetWithDefault.GetDefaultXenotype(factionDef2?.xenotypeSet))
            {
                if (PawnGenerator.tmpXenotypeChances.ContainsKey(xenotypeChance.xenotype))
                {
                    PawnGenerator.tmpXenotypeChances[xenotypeChance.xenotype] += xenotypeChance.chance;
                }
                else
                {
                    PawnGenerator.tmpXenotypeChances.Add(xenotypeChance.xenotype, xenotypeChance.chance);
                }
            }
        }
    }
}
