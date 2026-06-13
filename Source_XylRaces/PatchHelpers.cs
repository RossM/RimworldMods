using System.Linq.Expressions;
using RimWorld.Planet;
using XylXenos.Patches;

namespace XylXenos;

public static class PatchHelpers
{
    public enum DominantParent
    {
        None,
        Mother,
        Father,
    }

    public static int BiostatMetForDisplayBonus(this GeneDef geneDef)
    {
        var bonusGenes = geneDef.DefExt?.bonusGenes;
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
        var defExt = geneDef.DefExt;
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
            var modExtension = def.GetModExtension<DefModExtension_ThingOrRecipe_GeneDependent>();
            return modExtension != null && (modExtension.genePrerequisitesAny ?? Enumerable.Empty<GeneDef>()).Contains(geneDef);
        }).ToList();
        if (recipeDefs.Any())
        {
            yield return
                $"{"XylNewRecipes".Translate()}: {recipeDefs.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
        }

        IEnumerable<ThingDef> thingDefs = DefDatabase<RecipeDef>.AllDefsListForReading
            .SelectMany(def => def.products ?? Enumerable.Empty<ThingDefCountClass>(), (_, c) => c.thingDef)
            .Where(def => def.GetModExtension<DefModExtension_ThingOrRecipe_GeneDependent>()?.genePrerequisitesAny?.Contains(geneDef) ==
                          true)
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

    public static float ConversionPowerFactor_OffsetFromXenotype(Pawn pawn, bool invert, StringBuilder sb, Pawn recipient)
    {
        float result = 0f;

        Ideo ideo = pawn.Ideo;
        if (ideo == null)
            return 0f;

        XenotypeDef recipientXenotype = recipient.genes?.Xenotype;
        var xenotypeDefExtension = recipientXenotype?.GetModExtension<DefModExtension_Xenotype>();
        if (xenotypeDefExtension == null)
            return 0f;

        string text = "";

        if (!xenotypeDefExtension.agreeingMemes.NullOrEmpty())
        {
            foreach (MemeDef meme in ideo.memes)
            {
                if (xenotypeDefExtension.agreeingMemes.Contains(meme))
                {
                    float offset = invert ? -0.2f : 0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype, offset);
                }
            }
        }

        if (!xenotypeDefExtension.disagreeingMemes.NullOrEmpty())
        {
            foreach (MemeDef meme in ideo.memes)
            {
                if (xenotypeDefExtension.disagreeingMemes.Contains(meme))
                {
                    float offset = invert ? 0.2f : -0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype, offset);
                }
            }
        }

        if (sb != null && !text.NullOrEmpty())
            sb.AppendInNewLine($" -  {"AbilityIdeoConvertBreakdownPawnIdeo".Translate(pawn.Named("PAWN"))}: {text}");

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

        foreach (var designators in Faction.OfPlayer.AllPawns.Select(pawn => pawn.GeneTracker?.addDesignators))
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
        var defExt = geneDef.DefExt;
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
        outGene = pawn.genes?.GenesListForReading.FirstOrDefault(gene => gene.Active && gene.def.DefExt?.showInDrugPolicies == true);
        return outGene != null;
    }

    public static float GetJoyFactor(Pawn pawn, JoyGiver joyGiver)
    {
        List<JoyGiverFactor> joyGiverChanceFactors = pawn.GeneTracker?.joyGiverChanceFactors;
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
        if (gene?.DefExt?.femaleChance is not { } chance)
            return;

        pawn.gender = Rand.Chance(chance) ? Gender.Female : Gender.Male;
    }

    public static bool HasGenderRatio(GeneDef geneDef)
    {
        return geneDef.DefExt?.femaleChance != null;
    }

    public static void GenerateCongenitalHediffs(Pawn pawn)
    {
        foreach (GeneExt gene in pawn.ActiveGenesOfType<GeneExt>())
        {
            if (!gene.DefExt.congenitalHediffs.NullOrEmpty())
            {
                foreach (var congenitalHediff in gene.DefExt.congenitalHediffs)
                {
                    congenitalHediff.EventOccurred(pawn);
                }
            }
        }
    }

    public static float GenerateDistinctiveFactionColor(Faction faction, IEnumerable<Faction> allFactions)
    {
        const int candidateCount = 21;

        List<Color> factionColors = allFactions.Select(otherFaction => otherFaction.Color).ToList();

        float bestColorFromSpectrum = 0f;
        float bestDistanceMin = -1f;

        for (int i = 0; i < candidateCount; i++)
        {
            float colorFromSpectrum = i / (float)(candidateCount - 1);
            float distanceMin = float.MaxValue;
            Color color = ColorsFromSpectrum.Get(faction.def.colorSpectrum, colorFromSpectrum);

            foreach (Color otherColor in factionColors)
                distanceMin = Mathf.Min(distanceMin, ColorDistance(color, otherColor));

            if (distanceMin > bestDistanceMin)
            {
                bestColorFromSpectrum = colorFromSpectrum;
                bestDistanceMin = distanceMin;
            }
        }

        return bestColorFromSpectrum;

        // This is a simple approximate perceptual color distance function
        static float ColorDistance(Color a, Color b)
        {
            Color diff = a - b;
            return diff.r * diff.r + 2 * diff.g * diff.g + diff.b * diff.b;
        }
    }

    public static void ReassignFactionColors(PlanetLayer layer)
    {
        // Ensure we don't alter world generation
        Rand.PushState();

        try
        {
            List<Faction> allFactions = Find.FactionManager.AllFactionsListForReading
                .Where(faction => CanExistOnLayer(layer, faction.def)).ToList();
            List<Faction> shuffledFactions = allFactions.Where(faction => faction.def.colorSpectrum != null).ToList();
            shuffledFactions.Shuffle();

            for (int iter = 0; iter < 3; iter++)
            {
                foreach (var faction in shuffledFactions)
                {
                    faction.colorFromSpectrum
                        = GenerateDistinctiveFactionColor(faction, allFactions.Where(otherFaction => otherFaction != faction));
                }
            }
        }
        finally
        {
            Rand.PopState();
        }

        return;

        static bool CanExistOnLayer(PlanetLayer layer, FactionDef f)
        {
            if (!f.layerBlacklist.NullOrEmpty() && f.layerBlacklist.Contains(layer.Def))
                return false;
            if (!f.layerWhitelist.NullOrEmpty() || !layer.IsRootSurface)
                return f.layerWhitelist.Contains(layer.Def);
            return true;
        }
    }

    [UsedFromReflection]
    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        if (pawn.CurJobDef == DefOf.XylTakeShower && !pawn.pather.Moving)
        {
            flags &= ~(PawnRenderFlags.Clothes | PawnRenderFlags.Headgear);
        }

        return flags;
    }

    public static DominantParent GetDominantParent(Pawn mother, Pawn father)
    {
        int fatherStrength = XenotypeStrength(father);
        int motherStrength = XenotypeStrength(mother);

        if (fatherStrength > motherStrength)
            return DominantParent.Father;
        if (motherStrength > fatherStrength)
            return DominantParent.Mother;

        return DominantParent.None;
    }

    public static void CopyXenotype(Pawn destination, Pawn source)
    {
        destination.genes.SetXenotypeDirect(source.genes.Xenotype);
        destination.genes.xenotypeName = source.genes.xenotypeName;
        destination.genes.iconDef = source.genes.iconDef;
    }

    private static int XenotypeStrength(Pawn pawn)
    {
        if (pawn?.genes == null)
            return int.MinValue;

        return pawn.ActiveGenesOfType<GeneExt>().Sum(gene => gene.DefExt.xenotypeStrength);
    }

    public static bool HyperlactatingPrisonerInRoomCanProduce(Room r, ThingDef thingDef)
    {
        if (r is not { IsPrisonCell: true })
            return false;
        foreach (Pawn owner in r.Owners)
        {
            if (owner.FirstActiveGeneOfType<Gene_Hyperlactation>()?.def.DefExt?.hyperlactation?.item == thingDef)
                return true;
        }

        return false;
    }

    public static void RunDefGenerators(bool hotReload)
    {
        foreach (var type in GenTypes.AllTypesWithAttribute<DefGeneratorAttribute>())
        {
            try
            {
                Type defType = type.TryGetAttribute<DefGeneratorAttribute>().defType;

                var impliedDefsMethodInfo = type.GetMethod("ImpliedDefs");
                if (impliedDefsMethodInfo == null)
                {
                    Log.Error($"{type.Name} is marked as DefGenerator but doesn't have ImpliedDefs method");
                    continue;
                }

                var addDefsFn = (Action<IEnumerable<Def>, bool>)typeof(PatchHelpers).GetMethod(nameof(AddDefs))!.MakeGenericMethod(defType)
                    .CreateDelegate(typeof(Action<IEnumerable<Def>, bool>));
                var impliedDefsFn = (Func<bool, IEnumerable<Def>>)impliedDefsMethodInfo.CreateDelegate(typeof(Func<bool, IEnumerable<Def>>));

                addDefsFn(impliedDefsFn(hotReload), hotReload);
            }
            catch (Exception e)
            {
                Log.Error($"Error running def generator {type.AssemblyQualifiedName}: {e}");
            }
        }
    }

    public static void AddDefs<T>(IEnumerable<Def> defs, bool hotReload) where T : Def, new()
    {
        foreach (var def in defs)
        {
            DefGenerator.AddImpliedDef((T)def, hotReload);
        }
    }
}
