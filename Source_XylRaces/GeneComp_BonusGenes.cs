namespace XylXenos;

public class GeneCompProperties_BonusGenes : GeneCompProperties
{
    public GeneSetMaker maker;
    public float geneChance = 1.0f;
    public GeneType? addedGeneType;
    public bool removeAfterAdding = false;

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

    private GeneType AddedGeneType => Props.addedGeneType ?? parent.GeneType;

    public List<Gene> addedGenes = [];

    public override void CompExposeData()
    {
        Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
    }

    public override void CompPostPostAdd()
    {
        if (!Rand.Chance(Props.geneChance))
            return;

        var geneSet = Props.maker.Generate(Pawn, AddedGeneType);

        foreach (var gene in geneSet.GenesListForReading)
            AddGene(gene);

        if (Props.removeAfterAdding)
            Pawn.genes.RemoveGene(parent);
    }

    private void AddGene(GeneDef geneDef)
    {
        if (geneDef == null)
            return;
        if (Pawn.genes.GenesListForReading.Any(g => g.def == geneDef))
            return;

        addedGenes.Add(Pawn.genes.AddGene(geneDef, AddedGeneType == GeneType.Xenogene));
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
