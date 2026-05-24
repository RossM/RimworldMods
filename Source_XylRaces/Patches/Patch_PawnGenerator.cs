using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    public static class Patch_PawnGenerator
    {
        [Feature(typeof(GeneDefExtension_GenderRatio))]
        [InfixPrefix(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
        [InfixPatch("TryGenerateNewPawnInternal")]
        public static void GiveAppropriateBioAndNameTo_Prefix(
            Pawn pawn,
            PawnGenerationRequest request,
            XenotypeDef xenotype)
        {
            GeneHelpers.ModifyGenderByGenes(pawn, request, xenotype);
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
        [InfixPostfix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
        [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
        public static void XenotypeDefOf_Baseliner_Postfix(FactionDef factionDef, Faction faction, ref XenotypeDef __result)
        {
            __result = ((faction?.def ?? factionDef)?.xenotypeSet).GetDefaultXenotype();
        }

        [Feature(typeof(XenotypeSetWithDefault))]
        [InfixPrefix(typeof(PawnGenerator), "<XenotypesAvailableFor>g__AddOrAdjust|49_0")]
        [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
        public static bool AddOrAdjust_Prefix(XenotypeChance xenotypeChance, FactionDef factionDef, Faction faction)
        {
            if (xenotypeChance.xenotype != ((faction?.def ?? factionDef)?.xenotypeSet).GetDefaultXenotype())
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

            return false;
        }
    }
}
