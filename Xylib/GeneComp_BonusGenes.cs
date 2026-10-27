namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_BonusGenes : GeneCompProperties
{
    public required GeneSetMakerDef maker;
    public GeneType? addedGeneType;
    public bool removeAfterAdding = false;

    public GeneCompProperties_BonusGenes()
    {
        compClass = typeof(GeneComp_BonusGenes);
    }
}

[PublicAPI]
public class GeneComp_BonusGenes : GeneComp, IEventListener
{
    [Unsaved] private bool awaitingGeneGeneration;

    public GeneCompProperties_BonusGenes Props => (GeneCompProperties_BonusGenes)props;

    private GeneType AddedGeneType => Props.addedGeneType ?? parent.GeneType;

    public List<Gene>? addedGenes = [];

    public override void CompExposeData()
    {
        Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
    }

    public override void CompPostPostAdd()
    {
        if (PawnExtraData<GeneGenerationData>.Get(Pawn).InProgress)
        {
            awaitingGeneGeneration = true;
            return;
        }

        AddBonusGenes();
    }

    public void Notify_PostGenerateGenes()
    {
        if (!awaitingGeneGeneration)
            return;

        awaitingGeneGeneration = false;
        AddBonusGenes();
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostGenerateGenes, Pawn, Notify_PostGenerateGenes);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
        awaitingGeneGeneration = false;
    }

    private void AddBonusGenes()
    {
        DebugAssert.NotNull(Pawn.genes);

        var geneSet = Props.maker.Generate(Pawn, AddedGeneType);

        foreach (var gene in geneSet.GenesListForReading)
            AddGene(gene);

        if (Props.removeAfterAdding)
            Pawn.genes.RemoveGene(parent);
    }

    private void AddGene(GeneDef? geneDef)
    {
        DebugAssert.NotNull(Pawn.genes);

        if (geneDef == null)
            return;
        if (Pawn.genes.GenesListForReading.Any(g => g.def == geneDef))
            return;

        bool xenogene = AddedGeneType == GeneType.Xenogene;
        var genes = xenogene ? Pawn.genes.Xenogenes : Pawn.genes.Endogenes;
        // Remove conflicts before adding so the new gene wins regardless of vanilla override priority.
        foreach (var gene in genes.Where(g => geneDef.ConflictsWith(g.def)).ToArray())
            Pawn.genes.RemoveGene(gene);

        addedGenes ??= [];
        addedGenes.Add(Pawn.genes.AddGene(geneDef, xenogene));
    }

    public override void CompPostPostRemove()
    {
        DebugAssert.NotNull(Pawn.genes);

        if (Props.removeAfterAdding)
            return;
        if (addedGenes == null)
            return;

        foreach (var gene in addedGenes)
            Pawn.genes.RemoveGene(gene);
    }
}
