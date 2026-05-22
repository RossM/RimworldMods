using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    public static class Patch_PawnGenerator
    {
        [Feature(typeof(GeneDefExtension_GenderRatio))]
        [WrappedMember(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
        [InfixPatch("TryGenerateNewPawnInternal")]
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

        [Feature(typeof(GeneDefExtension_CongenitalHediff))]
        [HarmonyPostfix]
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

        [Feature(typeof(XenotypeSetWithDefault))]
        [WrappedMember(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(FactionDef factionDef = null, Faction faction = null)
        {
            FactionDef factionDef2 = faction?.def ?? factionDef;
            return XenotypeSetWithDefault.GetDefaultXenotype(factionDef2?.xenotypeSet);
        }

        [Feature(typeof(XenotypeSetWithDefault))]
        [WrappedMember(typeof(PawnGenerator), "<XenotypesAvailableFor>g__AddOrAdjust|49_0")]
        [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
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
