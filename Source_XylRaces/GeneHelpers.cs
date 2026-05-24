using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RimWorld;
using Verse;
using XylXenos.Genes;
using XylXenos.Patches;

namespace XylXenos;

public static class GeneHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Gene> GenesOfDef(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesWithDef(def);
    }

    // This is faster than pawn.genes.HasActiveGene(def) because it caches
    // the gene lookup.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGene(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null || def == null)
            return false;

        foreach (Gene g in pawn.GenesOfDef(def))
        {
            if (g.Active)
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> GenesOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesOfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ActiveGenesOfType<T>(this Pawn pawn) where T : class
    {
        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active)
                yield return g;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FirstActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active)
                return g;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return false;

        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active)
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGeneOfType<T>(this Pawn pawn, Func<T, bool> predicate) where T : class
    {
        if (pawn.genes == null)
            return false;

        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active && predicate(g))
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Gene> GenesWithModExtension<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesWithModExtension<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ActiveGeneDefExtensionsOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GenesWithModExtension<T>().Where(g => g.Active).SelectMany(g => g.def.modExtensions.OfType<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGeneDefExtensionOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return false;

        return pawn.GenesWithModExtension<T>().Any(g => g.Active);
    }

    public static int BiostatMetForDisplayBonus(this GeneDef geneDef)
    {
        var bonusGeneDefExt = geneDef.GetModExtension<GeneDefExtension_BonusGene>();
        if (bonusGeneDefExt == null)
            return 0;
        if (bonusGeneDefExt.geneChance < 1.0f)
            return 0;
        if (!bonusGeneDefExt.allowedGenes.NullOrEmpty())
            return bonusGeneDefExt.allowedGenes.Min(g => g.biostatMet);
        if (bonusGeneDefExt.biostatMet.Includes(0))
            return 0;
        return bonusGeneDefExt.biostatMet.min;
    }

    public static IEnumerable<string> GetGeneEffectDescriptions(this GeneDef gene)
    {
        if (gene is GeneDefExt ext)
        {
            foreach (var customEffectDescription in ext.CustomEffectDescriptions)
                yield return customEffectDescription;
        }

        if (!gene.modExtensions.NullOrEmpty())
        {
            foreach (var geneDefExtension in gene.modExtensions.OfType<GeneDefExtension>())
            {
                foreach (var customEffectDescription in geneDefExtension.CustomEffectDescriptions)
                    yield return customEffectDescription;
            }
        }

        // Official content doesn't need our help
        if (gene.modContentPack?.IsOfficialMod == true)
            yield break;

        IEnumerable<RecipeDef> recipeDefs = DefDatabase<RecipeDef>.AllDefsListForReading.Where(def =>
        {
            var modExtension = def.GetModExtension<DefModExtension_GeneDependent>();
            return modExtension != null && (modExtension.genePrerequisitesAny ?? Enumerable.Empty<GeneDef>()).Contains(gene);
        }).ToList();
        if (recipeDefs.Any())
        {
            yield return
                $"{"XylNewRecipes".Translate()}: {recipeDefs.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
        }

        IEnumerable<ThingDef> thingDefs = DefDatabase<RecipeDef>.AllDefsListForReading
            .SelectMany(def => def.products ?? Enumerable.Empty<ThingDefCountClass>(), (_, c) => c.thingDef)
            .Where(def => def.GetModExtension<DefModExtension_GeneDependent>()?.genePrerequisitesAny?.Contains(gene) == true)
            .ToList();
        if (thingDefs.Any())
        {
            yield return $"{"XylNewRecipes".Translate()}: {thingDefs.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
        }

        IEnumerable<MentalBreakDef> mentalBreakDefs
            = DefDatabase<MentalBreakDef>.AllDefsListForReading.Where(def => def.requiredGene == gene).ToList();
        foreach (var mentalBreakDef in mentalBreakDefs)
        {
            yield return $"{"XylPossibleMentalBreak".Translate()}: {mentalBreakDef.mentalState.LabelCap}";
        }
    }

    public static IEnumerable<StatDrawEntry> GetGeneSpecialDisplayStats(this GeneDef gene)
    {
        if (!gene.modExtensions.NullOrEmpty())
        {
            foreach (var geneDefExtension in gene.modExtensions.OfType<GeneDefExtension>())
            {
                foreach (var specialDisplayStatEntry in geneDefExtension.SpecialDisplayStats)
                    yield return specialDisplayStatEntry;
            }
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

        foreach (var defExtension_designator in Faction.OfPlayer.GetPawns()
                     .SelectMany(pawn => pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Designator>()))
        {
            geneDesignators.AddRange(defExtension_designator.addDesignators.Where(def => def.designationCategory == __instance)
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

    public static bool GeneShouldBeVisible(GeneDef geneDef, bool inheritable)
    {
        return geneDef.GetModExtension<GeneDefExtension_UIFilter>()?.ShouldBeVisible(inheritable) != false;
    }

    public static bool TryGetChemicalDependencyGene(Pawn pawn, out Gene gene)
    {
        gene = pawn.genes?.GenesListForReading.FirstOrDefault(g => g.def is GeneDefExt { showInDrugPolicies: true });
        return gene != null;
    }

    public static float GetJoyFactor(Pawn pawn, JoyGiver joyGiver)
    {
        float factor = 1f;
        foreach (var joyGiverFactor in pawn.GetComp<CompPawn_GeneSet>().joyGiverChanceFactors)
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
        if (gene == null)
            return;

        pawn.gender = gene.GetModExtension<GeneDefExtension_GenderRatio>().GetGender();
    }

    public static bool HasGenderRatio(GeneDef gene)
    {
        return gene.GetModExtension<GeneDefExtension_GenderRatio>() != null;
    }

    public static IEnumerable<GeneDefExt> ExtendedGeneDefs(this Pawn pawn)
    {
        if (pawn.genes == null)
            return Enumerable.Empty<GeneDefExt>();
        return pawn.genes.GenesListForReading.Where(gene => gene.Active).Select(gene => gene.def).OfType<GeneDefExt>();
    }

    public static void GenerateCongenitalHediffs(Pawn pawn)
    {
        foreach (var def in pawn.ExtendedGeneDefs())
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

    public static void TickIntervalExt(this Gene gene, int delta)
    {
        if (!gene.Active)
            return;
        if (gene.def is not GeneDefExt def)
            return;
        if (def.hediffGivers.NullOrEmpty())
            return;
        if (!gene.pawn.IsHashIntervalTick(60, delta))
            return;

        foreach (var hediffGiver in def.hediffGivers)
        {
            hediffGiver.OnIntervalPassed(gene.pawn, null);
        }
    }
}
