using RimWorld.Planet;

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
        var bonusGenes = geneDef.CompProps<GeneCompProperties_BonusGenes>();
        return bonusGenes?.maker.root.BiostatMetForDisplay ?? 0;
    }

    public static float ConversionPowerFactor_OffsetFromXenotype(Pawn pawn, bool invert, StringBuilder? sb, Pawn recipient)
    {
        float result = 0f;

        Ideo? ideo = pawn.Ideo;
        if (ideo == null)
            return 0f;

        XenotypeDef? recipientXenotype = recipient.genes?.Xenotype;
        var xenotypeDefExtension = recipientXenotype?.GetModExtension<DefModExtension_Xenotype>();
        if (xenotypeDefExtension == null)
            return 0f;

        string text = "";

        if (xenotypeDefExtension.agreeingMemes is { Count: > 0 })
        {
            foreach (MemeDef meme in ideo.memes)
            {
                if (xenotypeDefExtension.agreeingMemes.Contains(meme))
                {
                    float offset = invert ? -0.2f : 0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype!, offset);
                }
            }
        }

        if (xenotypeDefExtension.disagreeingMemes is { Count: > 0 })
        {
            foreach (MemeDef meme in ideo.memes)
            {
                if (xenotypeDefExtension.disagreeingMemes.Contains(meme))
                {
                    float offset = invert ? 0.2f : -0.2f;
                    result += offset;
                    text += MemeAndXenotypeDesc(meme, recipientXenotype!, offset);
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

    public static float GenerateDistinctiveFactionColor(Faction faction, IEnumerable<Faction> allFactions)
    {
        const int candidateCount = 21;

        List<Color> factionColors = allFactions.Select(otherFaction => otherFaction.Color).ToList();

        float bestColorFromSpectrum = 0f;
        float bestDistanceMin = -1f;

        for (int i = 0; i < candidateCount; i++)
        {
            DebugAssert.NotNull(faction.def.colorSpectrum);

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
        DebugAssert.NotNull(Find.FactionManager);

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
            if (f.layerBlacklist is { Count: > 0 } && f.layerBlacklist.Contains(layer.Def))
                return false;
            if (f.layerWhitelist is { Count: > 0 } && !f.layerWhitelist.Contains(layer.Def))
                return false;
            return true;
        }
    }

    [UsedFromReflection]
    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        DebugAssert.NotNull(pawn.pather);

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
        DebugAssert.NotNull(destination.genes);
        DebugAssert.NotNull(source.genes);

        destination.genes.SetXenotypeDirect(source.genes.Xenotype);
        destination.genes.xenotypeName = source.genes.xenotypeName;
        destination.genes.iconDef = source.genes.iconDef;
    }

    private static int XenotypeStrength(Pawn pawn)
    {
        if (pawn.genes == null)
            return int.MinValue;

        return pawn.ActiveGenesOfType<GeneWithComps>()
            .Sum(gene => gene.DefExt.CompProps<GeneCompProperties_XenotypeStrength>()?.strength ?? 0);
    }

    public static bool HyperlactatingPrisonerInRoomCanProduce(Room? r, ThingDef thingDef)
    {
        if (r is not { IsPrisonCell: true })
            return false;
        foreach (Pawn owner in r.Owners)
        {
            if (owner.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>()?.Props.item == thingDef)
                return true;
        }

        return false;
    }

    public static bool IsUsingEcholocation(Thing caster)
    {
        return caster is Pawn pawn && pawn.HasActiveGene(DefOf.XylEcholocation) && PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn)
               && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.2f;
    }

    public static float FoodOptimalityBonus(Pawn eater, Thing foodSource)
    {
        // Check if this food satisfies a diet dependency
        float extra = 0f;
        foreach (var hediff in eater.HediffsOfType<Hediff_DietDependency>())
        {
            if (hediff.ValidateFood(foodSource) && hediff.ShouldSatisfy)
                extra += 100f;
        }

        return extra;
    }

    public static void FixupChemicalGenes()
    {
        foreach (var geneDef in DefDatabase<GeneDef>.AllDefs)
        {
            if (geneDef.chemical is not { } chemical)
                continue;
            if (chemical.GetModExtension<DefModExtension_Chemical>() is not { } defExtension)
                continue;

            if (defExtension.requiredGenesAll is { Count: > 0 })
                geneDef.prerequisite = defExtension.requiredGenesAll[0];
            else if (defExtension.requiredGenesAny is { Count: 1 })
                geneDef.prerequisite = defExtension.requiredGenesAny[0];
        }
    }

    public static bool IsThoughtFromIngestionDisallowedByGenes(
        Pawn eater,
        ThoughtDef? thought,
        ThingDef? ingestible)
    {
        if (thought == null || ingestible == null)
        {
            return false;
        }

        List<GeneIngestionThoughtOverride>? thoughtOverrides = eater.GeneTracker_XylXenos?.ingestionThoughtOverrides;
        if (thoughtOverrides == null)
            return false;

        foreach (var thoughtOverride in thoughtOverrides)
        {
            if (thoughtOverride.thing != null && thoughtOverride.thing != ingestible)
                continue;

            IEnumerable<FoodGroupDef> foodGroups = ingestible.FoodGroups.ToList();
            if (thoughtOverride.allowedFoodGroups is { Count: > 0 } && !foodGroups.Intersect(thoughtOverride.allowedFoodGroups).Any())
                continue;
            if (thoughtOverride.disallowedFoodGroups is { Count: > 0 } && foodGroups.Intersect(thoughtOverride.disallowedFoodGroups).Any())
                continue;

            if (thoughtOverride.thoughts is { Count: > 0 } && !thoughtOverride.thoughts.Contains(thought))
                continue;

            return true;
        }

        return false;
    }

    public static float GetJoyFactor(Pawn pawn, JoyGiver joyGiver)
    {
        List<JoyGiverFactor>? joyGiverChanceFactors = pawn.GeneTracker_XylXenos?.joyGiverChanceFactors;
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

    public static bool ShouldGetGeneticPassion(Pawn pawn, SkillRecord record, int minorPassions)
    {
        if (minorPassions <= 0)
            return false;

        if (ModsConfig.BiotechActive && pawn.genes != null)
        {
            foreach (Gene item2 in pawn.genes.GenesListForReading)
            {
                if (item2.Active && item2.def.passionMod is { modType: PassionMod.PassionModType.AddOneLevel } &&
                    item2.def.passionMod.skill == record.def)
                    return true;
            }
        }

        return false;
    }

    public static void SortColorGenes(List<GeneDef> list)
    {
        static bool IsSkinColor(GeneDef g) => g.displayCategory == DefOf.Cosmetic_Skin && g.skinColorOverride.HasValue;

        var colorGenes = list.Where(IsSkinColor).ToList();
        SortByColor(colorGenes, g => g.skinColorOverride!.Value);

        int i = 0;
        for (int j = 0; j < list.Count; j++)
        {
            if (IsSkinColor(list[j]))
            {
                list[j] = colorGenes[i];
                i++;
            }
        }
    }

    public static void SortByColor<T>(List<T> colorDefs, Func<T, Color> getColor)
    {
        colorDefs.SortBy(x =>
        {
            Color.RGBToHSV(getColor(x), out var H, out var S, out _);
            return S >= 0.05f ? Mathf.RoundToInt(H * 100f) : 200f;
        }, x =>
        {
            Color.RGBToHSV(getColor(x), out _, out _, out var V);
            return Mathf.RoundToInt(V * 100f);
        });
    }
}
