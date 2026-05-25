using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class PatchHelpers
{
    public static int BiostatMetForDisplayBonus(this GeneDef geneDef)
    {
        var bonusGenes = geneDef.DefExt()?.bonusGenes;
        if (bonusGenes == null)
            return 0;
        if (bonusGenes.geneChance < 1.0f)
            return 0;
        if (!bonusGenes.allowedGenes.NullOrEmpty())
            return bonusGenes.allowedGenes.Min(g => g.biostatMet);
        if (bonusGenes.biostatMet.Includes(0))
            return 0;
        return bonusGenes.biostatMet.min;
    }

    public static IEnumerable<string> GetGeneEffectDescriptions(this GeneDef geneDef)
    {
        var defExt = geneDef.DefExt();
        if (defExt != null)
        {
            foreach (var customEffectDescription in defExt.CustomEffectDescriptions)
                yield return customEffectDescription;
        }

        // Official content doesn't need our help
        if (geneDef.modContentPack?.IsOfficialMod == true)
            yield break;

        IEnumerable<RecipeDef> recipeDefs = DefDatabase<RecipeDef>.AllDefsListForReading.Where(def =>
        {
            var modExtension = def.GetModExtension<DefModExtension_GeneDependent>();
            return modExtension != null && (modExtension.genePrerequisitesAny ?? Enumerable.Empty<GeneDef>()).Contains(geneDef);
        }).ToList();
        if (recipeDefs.Any())
        {
            yield return
                $"{"XylNewRecipes".Translate()}: {recipeDefs.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
        }

        IEnumerable<ThingDef> thingDefs = DefDatabase<RecipeDef>.AllDefsListForReading
            .SelectMany(def => def.products ?? Enumerable.Empty<ThingDefCountClass>(), (_, c) => c.thingDef)
            .Where(def => def.GetModExtension<DefModExtension_GeneDependent>()?.genePrerequisitesAny?.Contains(geneDef) == true)
            .ToList();
        if (thingDefs.Any())
        {
            yield return $"{"XylNewRecipes".Translate()}: {thingDefs.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
        }

        IEnumerable<MentalBreakDef> mentalBreakDefs
            = DefDatabase<MentalBreakDef>.AllDefsListForReading.Where(def => def.requiredGene == geneDef).ToList();
        foreach (var mentalBreakDef in mentalBreakDefs)
        {
            yield return $"{"XylPossibleMentalBreak".Translate()}: {mentalBreakDef.mentalState.LabelCap}";
        }
    }

    public static float ConversionPowerFactor_OffsetFromXenotype(Pawn pawn, Pawn recipient, bool invert, StringBuilder sb)
    {
        float result = 0;
        string text = string.Empty;
        XenotypeDef recipientXenotype = recipient.genes?.Xenotype;
        if (recipientXenotype == null)
            return 0;

        var agreeingMemes = recipientXenotype.GetModExtension<XenotypeDefExtension>()?.agreeingMemes;
        if (agreeingMemes != null)
        {
            foreach (MemeDef meme in pawn.Ideo.memes)
            {
                if (agreeingMemes.Contains(meme))
                {
                    float offset = invert ? -0.2f : 0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype, offset);
                }
            }
        }

        var disagreeingMemes = recipientXenotype.GetModExtension<XenotypeDefExtension>()?.disagreeingMemes;
        if (disagreeingMemes != null)
        {
            foreach (MemeDef meme in pawn.Ideo.memes)
            {
                if (disagreeingMemes.Contains(meme))
                {
                    float offset = invert ? 0.2f : -0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype, offset);
                }
            }
        }

        if (sb != null && !text.NullOrEmpty())
        {
            sb.AppendInNewLine($" -  {"AbilityIdeoConvertBreakdownPawnIdeo".Translate(pawn.Named("PAWN"))}: {text}");
        }

        return result;

        string MemeAndXenotypeDesc(MemeDef meme, XenotypeDef xenotype, float offset)
        {
            if (sb == null)
            {
                return string.Empty;
            }

            // Adding 1 to the offset and reporting it as a percentage is complete nonsense and gives the impression
            // that these are factors being multiplied together rather than added. However, it's complete nonsense
            // that matches what the base game does for traits, so I am holding my nose and matching it.
            return
                $"\n   -  {"XylAbilityIdeoConvertBreakdownMemeVsXenotype".Translate(meme.label.Named("MEME"), xenotype.label.Named("XENOTYPE")).CapitalizeFirst()}: {(1f + offset).ToStringPercent()}";
        }
    }

    public static void AddDesignators(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        HashSet<Designator> geneDesignators = [];

        foreach (var designators in Faction.OfPlayer.GetPawns().Select(pawn => pawn.GeneSet()?.addDesignators))
        {
            if (designators == null)
                continue;

            geneDesignators.AddRange(designators.Where(def => def.designationCategory == __instance)
                .Select(GetCachedDesignator));
        }

        if (geneDesignators.Any())
            __result = __result.Concat(geneDesignators);

        Designator GetCachedDesignator(BuildableDef def)
        {
            DesignationCategoryDef.BuildablePreceptBuilding key = new DesignationCategoryDef.BuildablePreceptBuilding(def, null);
            if (!__instance.ideoBuildingDesignatorsCached.TryGetValue(key, out var value))
            {
                value = new Designator_Build(def);
                __instance.ideoBuildingDesignatorsCached[key] = value;
            }

            return value;
        }
    }

    public static bool GeneShouldBeVisible(GeneDef geneDef, GeneType geneType)
    {
        var defExt = geneDef.DefExt();
        if (defExt == null)
            return true;

        if (!defExt.showInXenotypeCreation)
            return false;
        if (defExt.geneType != null && defExt.geneType != geneType)
            return false;

        return true;
    }

    public static bool TryGetChemicalDependencyGene(Pawn pawn, out Gene outGene)
    {
        outGene = pawn.genes?.GenesListForReading.FirstOrDefault(gene => gene.Active && gene.DefExt()?.showInDrugPolicies == true);
        return outGene != null;
    }

    public static float GetJoyFactor(Pawn pawn, JoyGiver joyGiver)
    {
        List<JoyGiverFactor> joyGiverChanceFactors = pawn.GeneSet()?.joyGiverChanceFactors;
        if (joyGiverChanceFactors == null)
            return 1f;

        float factor = 1f;
        foreach (var joyGiverFactor in joyGiverChanceFactors)
        {
            if (joyGiverFactor.joyGiver == joyGiver.def)
                factor *= joyGiverFactor.factor;
        }

        return factor;
    }

    public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef xenotype)
    {
        if (request.FixedGender != null)
            return;

        GeneDef gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ??
                       xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
        if (gene?.DefExt()?.femaleChance is not { } chance)
            return;

        pawn.gender = Rand.Chance(chance) ? Gender.Female : Gender.Male;
    }

    public static bool HasGenderRatio(GeneDef geneDef)
    {
        return geneDef.DefExt()?.femaleChance != null;
    }

    public static void GenerateCongenitalHediffs(Pawn pawn)
    {
        foreach (var def in pawn.ActiveDefExts())
        {
            if (!def.congenitalHediffs.NullOrEmpty())
            {
                foreach (var congenitalHediff in def.congenitalHediffs)
                {
                    congenitalHediff.EventOccurred(pawn);
                }
            }
        }
    }
}