namespace XylXenos;

public class GeneCompProperties_BonusGenes : GeneCompProperties
{
    public IntRange biostatArc = IntRange.Zero;
    public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
    public IntRange biostatMet = new(int.MinValue, int.MaxValue);
    public float geneChance = 1.0f;
    public GeneType? addedGeneType;
    [CanBeNull] public List<GeneDef> allowedGenes;
    [CanBeNull] public List<GeneDef> prohibitedGenes;
    public bool removeAfterAdding = false;
    public bool ignoreSelectionWeight = false;
    public IntRange count = IntRange.One;

    public GeneCompProperties_BonusGenes()
    {
        compClass = typeof(GeneComp_BonusGenes);
    }
}

[UsedFromXml]
public class GeneComp_BonusGenes : GeneComp
{
    [NotNull]
    public GeneCompProperties_BonusGenes Props => (GeneCompProperties_BonusGenes)props;

    public List<Gene> addedGenes = [];

    public override void CompExposeData()
    {
        Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
    }

    public override void CompPostPostAdd()
    {
        if (!Rand.Chance(Props.geneChance))
            return;

        int count = Props.count.RandomInRange;

        for (int i = 0; i < count; i++)
        {
            List<GeneDef> genes = !Props.allowedGenes.NullOrEmpty()
                ? Props.allowedGenes
                : DefDatabase<GeneDef>.AllDefsListForReading;
            if (genes.TryRandomElementByWeight(GeneWeight, out GeneDef geneDef))
                AddGene(geneDef);
        }

        if (Props.removeAfterAdding)
            Pawn.genes.RemoveGene(parent);
    }

    private void AddGene(GeneDef geneDef)
    {
        if (geneDef == null)
            return;
        if (Pawn.genes.GenesListForReading.Any(g => g.def == geneDef))
            return;

        if (!GeneTuning.BiostatRange.Includes(geneDef.biostatMet +
                                              Pawn.genes.GenesListForReading.Sum(g => g.Active ? g.def.biostatMet : 0)))
            return;

        var geneType = Props.addedGeneType ?? parent.GeneType;
        addedGenes.Add(Pawn.genes.AddGene(geneDef, geneType == GeneType.Xenogene));
    }

    private float GeneWeight(GeneDef geneDef)
    {
        if (!geneDef.canGenerateInGeneSet)
            return 0.0f;

        if (geneDef.modContentPack != null && Config.Instance.ignoreGenesFromMods.Contains(geneDef.modContentPack.PackageId))
            return 0.0f;

        if (!Props.prohibitedGenes.NullOrEmpty() && Props.prohibitedGenes.Contains(geneDef))
            return 0.0f;

        if (!Props.biostatArc.Includes(geneDef.biostatArc))
            return 0.0f;
        if (!Props.biostatCpx.Includes(geneDef.biostatCpx))
            return 0.0f;
        if (!Props.biostatMet.Includes(geneDef.biostatMet))
            return 0.0f;

        if (geneDef.Extension_GeneWithComps is { } defExt)
        {
            if (defExt.gender != null && defExt.gender != Pawn.gender)
                return 0.0f;
            if (defExt.geneType != null && defExt.geneType != parent.GeneType)
                return 0.0f;
        }

        // Aptitude-giving genes must not apply to only disabled skills
        if (!geneDef.aptitudes.NullOrEmpty() && geneDef.aptitudes.All(aptitude => Pawn.skills.GetSkill(aptitude.skill).TotallyDisabled))
            return 0.0f;

        // No genes with requirements, unless they are met by the pawn's xenotype or already added genes
        if (geneDef.prerequisite != null && !Pawn.genes.Xenotype.AllGenes.Contains(geneDef.prerequisite) &&
            !addedGenes.Any(g => g.def == geneDef.prerequisite))
        {
            return 0.0f;
        }

        // No genes that conflict with genes in the pawn's xenotype or already added genes
        foreach (var gene in Pawn.genes.Xenotype.AllGenes)
        {
            if (geneDef == gene)
                return 0.0f;

            if (geneDef.exclusionTags != null && gene.exclusionTags != null &&
                geneDef.exclusionTags.Intersect(gene.exclusionTags).Any())
            {
                return 0.0f;
            }
        }

        foreach (var gene in addedGenes)
        {
            if (geneDef == gene.def)
                return 0.0f;

            if (geneDef.exclusionTags != null && gene.def.exclusionTags != null &&
                geneDef.exclusionTags.Intersect(gene.def.exclusionTags).Any())
            {
                return 0.0f;
            }
        }

        return Props.ignoreSelectionWeight ? 1.0f : geneDef.selectionWeight;
    }

    public override void CompPostPostRemove()
    {
        if (Props.removeAfterAdding)
            return;
        if (addedGenes == null)
            return;

        foreach (var gene in addedGenes)
            Pawn.genes.RemoveGene(gene);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismChanceLabel".TranslateSimple(),
            Props.geneChance.ToStringPercent(), "XylAtavismChanceDesc".TranslateSimple(), 1002);
        if (addedGenes == null)
            yield break;
        string text = string.Join(", ", addedGenes.Select(g => g.Label)).CapitalizeFirst();
        yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismGenesLabel".TranslateSimple(),
            text, "XylAtavismGenesDesc".TranslateSimple(), 1001);
    }
}
