namespace XylXenos;

public static class DebugOutputs
{
    private static readonly Dictionary<string, string> specialAbbreviations = new()
    {
        { "combat", "cbt" },
        { "environment", "env" },
        { "global", "glb" },
        { "healing", "heal" },
        { "learning", "lrn" },
        { "medical", "med" },
        { "melee", "mel" },
        { "movement", "mov" },
        { "resistance", "res" },
        { "sensitivity", "sens" },
        { "social", "soc" },
        { "toxic", "tox" },
    };

    [DebugOutput("Genes")]
    public static void GeneDisplayOrder()
    {
        TableDataGetter<GeneDef>[] columns =
        [
            new("defName", geneDef => geneDef.defName),
            new("label", geneDef => geneDef.LabelCap),
            new("displayCategory", geneDef => geneDef.displayCategory.defName),
            new("displayPriorityInXenotype", geneDef => geneDef.displayCategory.displayPriorityInXenotype),
            new("displayPriorityInGenepack", geneDef => geneDef.displayCategory.displayPriorityInGenepack),
            new("displayOrderInCategory", geneDef => geneDef.displayOrderInCategory),
            new("exclusionTags", geneDef => geneDef.exclusionTags?.ToCommaList() ?? "")
        ];
        DebugTables.MakeTablesDialog(
            DefDatabase<GeneDef>.AllDefs.OrderByDescending(geneDef => geneDef.displayCategory.displayPriorityInXenotype)
                .ThenBy(geneDef => geneDef.displayOrderInCategory), columns);
    }

    [DebugOutput]
    public static void FactionXenotypes()
    {
        List<TableDataGetter<FactionDef>> columns =
        [
            new("defName", factionDef => factionDef.defName),
            new("label", factionDef => factionDef.LabelCap),
        ];
        foreach (var xenotypeDef in DefDatabase<XenotypeDef>.AllDefs)
        {
            var defCaptured = xenotypeDef;

            columns.Add(new(defCaptured.defName, factionDef =>
            {
                float xenotypeChance = GetXenotypeChance(factionDef, defCaptured);
                return xenotypeChance > 0 ? xenotypeChance.ToStringPercent() : "";
            }));
        }

        DebugTables.MakeTablesDialog(DefDatabase<FactionDef>.AllDefs, columns.ToArray());
        return;

        static float GetXenotypeChance(FactionDef factionDef, XenotypeDef xenotypeDef)
        {
            var weights = PawnGenerator.XenotypesAvailableFor(PawnKindDefOf.Colonist, factionDef);
            var totalWeight = weights.Sum(pair => pair.Value);
            if (weights.TryGetValue(xenotypeDef, out var xenotypeWeight))
                return xenotypeWeight / totalWeight;
            return 0f;
        }
    }

    [DebugOutput]
    public static void PawnKindXenotypes()
    {
        List<TableDataGetter<PawnKindDef>> columns =
        [
            new("defName", pawnKindDef => pawnKindDef.defName),
            new("label", pawnKindDef => pawnKindDef.LabelCap),
        ];
        foreach (var def in DefDatabase<XenotypeDef>.AllDefs)
        {
            var xenotypeDef = def;

            columns.Add(new(xenotypeDef.defName, pawnKindDef =>
            {
                float xenotypeChance = GetXenotypeChance(pawnKindDef, xenotypeDef);
                return xenotypeChance > 0 ? xenotypeChance.ToStringPercent() : "";
            }));
        }

        DebugTables.MakeTablesDialog(DefDatabase<PawnKindDef>.AllDefs.Where(pawnKindDef => pawnKindDef.xenotypeSet != null),
            columns.ToArray());
        return;

        static float GetXenotypeChance(PawnKindDef pawnKindDef, XenotypeDef xenotypeDef)
        {
            var weights = PawnGenerator.XenotypesAvailableFor(pawnKindDef, pawnKindDef.defaultFactionDef);
            var totalWeight = weights.Sum(pair => pair.Value);
            if (weights.TryGetValue(xenotypeDef, out var xenotypeWeight))
                return xenotypeWeight / totalWeight;
            return 0f;
        }
    }

    [DebugOutput("Genes")]
    public static void XenotypeSkillAptitudes()
    {
        List<TableDataGetter<XenotypeDef>> columns =
        [
            new("defName", xenotypeDef => xenotypeDef.defName),
            new("label", xenotypeDef => xenotypeDef.LabelCap),
        ];
        foreach (var def in DefDatabase<SkillDef>.AllDefs)
        {
            var skillDef = def;

            columns.Add(new(skillDef.LabelCap, xenotypeDef =>
            {
                int skillModifier = GetSkillModifier(xenotypeDef, skillDef);

                return skillModifier != 0 ? skillModifier.ToStringWithSign() : "";
            }));
        }

        DebugTables.MakeTablesDialog(DefDatabase<XenotypeDef>.AllDefs, columns.ToArray());
        return;

        static int GetSkillModifier(XenotypeDef xenotypeDef, SkillDef skillDef)
        {
            return xenotypeDef.genes
                .Where(gene => gene.aptitudes != null)
                .SelectMany(gene => gene.aptitudes)
                .Where(aptitude => aptitude.skill == skillDef)
                .Select(aptitude => aptitude.level)
                .Sum();
        }
    }

    [DebugOutput]
    public static void FactionMemes()
    {
        List<TableDataGetter<FactionDef>> columns =
        [
            new("defName", factionDef => factionDef.defName),
            new("label", factionDef => factionDef.LabelCap),
        ];
        foreach (var def in DefDatabase<MemeDef>.AllDefs.Where(memeDef => memeDef.category == MemeCategory.Normal)
                     .OrderBy(memeDef => memeDef.label))
        {
            var memeDef = def;

            columns.Add(new((string)memeDef.LabelCap, factionDef =>
            {
                if (factionDef.requiredMemes?.Contains(memeDef) is true)
                    return "Req";
                if (factionDef.allowedMemes is { Count: > 0 })
                    return factionDef.allowedMemes.Contains(memeDef) ? "\u2713" : "";
                if (factionDef.disallowedMemes is { Count: > 0 })
                    return factionDef.disallowedMemes.Contains(memeDef) ? "" : "\u2713";
                return "";
            }));
        }

        DebugTables.MakeTablesDialog(DefDatabase<FactionDef>.AllDefs.Where(ShouldShow), columns.ToArray());
        return;

        static bool ShouldShow(FactionDef factionDef) =>
            factionDef.requiredMemes is { Count: > 0 } ||
            factionDef.allowedMemes is { Count: > 0 } ||
            factionDef.disallowedMemes is { Count: > 0 };
    }

    [DebugOutput("Genes")]
    public static void DrugGeneRequirements()
    {
        TableDataGetter<ThingDef>[] columns =
        [
            new("defName", thingDef => thingDef.defName),
            new("label", thingDef => thingDef.LabelCap),
            new("prohibitedGenes",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.prohibitedGenes
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
            new("requiredGenesAll",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.requiredGenesAll
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
            new("requiredGenesAny",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.requiredGenesAny
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
        ];
        DebugTables.MakeTablesDialog(
            DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.IsDrug).OrderBy(thingDef => thingDef.BaseMarketValue), columns);
    }

    [DebugOutput]
    public static void DefsMissingModContentPack()
    {
        TableDataGetter<Def>[] columns =
        [
            new("defName", def => def.defName),
            new("class", def => def.GetType().FullName),
        ];

        HashSet<Def> missingDefs = [];

        foreach (var type in GenDefDatabase.AllDefTypesWithDatabases())
        {
            foreach (var def in GenDefDatabase.GetAllDefsInDatabaseForDef(type))
            {
                if (def.modContentPack == null)
                    missingDefs.Add(def);
            }
        }

        DebugTables.MakeTablesDialog(missingDefs.OrderBy(d => d.GetType().FullName).ThenBy(d => d.defName), columns);
    }

    [DebugOutput("Genes")]
    public static void XenotypeBaseStats()
    {
        StatDef[] stats =
        [
            // Movement
            StatDefOf.MoveSpeed,
            // Work and learning
            StatDefOf.WorkSpeedGlobal,
            StatDefOf.GlobalLearningFactor,
            StatDefOf.RestFallRateFactor,
            // Environment
            StatDefOf.ComfyTemperatureMin,
            StatDefOf.ComfyTemperatureMax,
            StatDefOf.ToxicResistance,
            StatDefOf.ToxicEnvironmentResistance,
            // Combat and healing
            StatDefOf.IncomingDamageFactor,
            StatDefOf.MeleeDamageFactor,
            StatDefOf.InjuryHealingFactor,
            StatDefOf.ImmunityGainSpeed,
            // Social and mental
            StatDefOf.PsychicSensitivity,
            StatDefOf.PawnBeauty,
        ];

        List<TableDataGetter<XenotypeDef>> columns =
        [
            new("defName", def => def.defName),
            new("label", def => def.LabelCap),
        ];

        foreach (var stat in stats)
        {
            DebugAssert.NotNull(stat.label);

            var localStat = stat;
            columns.Add(new(Abbreviate(stat.label).CapitalizeFirst(), def => BaseStatValue(def, localStat)));
        }

        DebugTables.MakeTablesDialog(DefDatabase<XenotypeDef>.AllDefs, columns.ToArray());

        static float BaseStatValue(XenotypeDef xenotype, StatDef stat)
        {
            float value = ThingDefOf.Human.GetStatValueAbstract(stat);
            float offset = 0f;
            float factor = 1f;

            foreach (var gene in xenotype.genes)
            {
                offset = gene.statOffsets?.Where(m => m.stat == stat).Aggregate(offset, (o, m) => o + m.value) ?? offset;
                factor = gene.statFactors?.Where(m => m.stat == stat).Aggregate(factor, (f, m) => f * m.value) ?? factor;

                if (gene.forcedTraits == null)
                    continue;

                foreach (var trait in gene.forcedTraits)
                {
                    DebugAssert.NotNull(trait.def.degreeDatas);

                    var degreeData = trait.def.degreeDatas.Single(d => d.degree == trait.degree);
                    offset = degreeData.statOffsets?.Where(m => m.stat == stat).Aggregate(offset, (o, m) => o + m.value) ?? offset;
                    factor = degreeData.statFactors?.Where(m => m.stat == stat).Aggregate(factor, (f, m) => f * m.value) ?? factor;
                }
            }

            return (value + offset) * factor;
        }
    }

    private static string Abbreviate(string str, int lettersPerWord = 3)
    {
        return string.Join(" ", str.Split(' ').Select(s => AbbreviateWord(s, lettersPerWord)));
    }

    private static string AbbreviateWord(string w, int maxLength)
    {
        if (specialAbbreviations.TryGetValue(w, out var result))
            return result;
        if (w.Length <= maxLength)
            return w;
        if (w.EndsWith("ing"))
            w = w[..^3] + "g";
        // ReSharper disable once StringLiteralTypo
        w = w[0] + w[1..].Where(c => !"aeiou".Contains(c)).Join(delimiter: "");
        if (w.Length <= maxLength)
            return w;
        return w[..maxLength];
    }
}
