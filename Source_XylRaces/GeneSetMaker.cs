namespace XylXenos;

public abstract class GeneSetMaker
{
    public virtual int BiostatMetForDisplay => 0;

    public float chance = 1f;
    public IntRange count = IntRange.One;
    public List<GeneDef> prohibitedGenes;

    public GeneSet Generate(Pawn pawn, GeneType geneType = GeneType.Xenogene)
    {
        var geneSet = new GeneSet();
        List<GeneDef> genes = geneSet.GenesListForReading;

        if (pawn.genes.Xenotype != null)
        {
            foreach (var gene in pawn.genes.Xenotype.genes)
                geneSet.AddGene(gene);
        }

        if (pawn.genes.CustomXenotype != null)
        {
            foreach (var gene in pawn.genes.CustomXenotype.genes)
                geneSet.AddGene(gene);
        }

        foreach (var gene in pawn.genes.GenesListForReading.Where(g => g.Active))
            geneSet.AddGene(gene.def);

        int existingGeneCount = genes.Count;

        Log.Message($"Before AddGenes: {genes.Select(g => g.label).ToCommaList()}");

        AddGenes(geneSet, geneType, pawn);

        Log.Message($"After AddGenes: {genes.Select(g => g.label).ToCommaList()}");

        var newGenes = new GeneSet();
        for (int i = existingGeneCount; i < genes.Count; i++)
            newGenes.AddGene(genes[i]);

        return newGenes;
    }

    public void AddGenes(GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        if (!Rand.Chance(chance))
            return;

        AddGenesInt(geneSet, geneType, pawn, count.RandomInRange);
    }

    protected virtual void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        for (int i = 0; i < countValue; i++)
        {
            if (!DefDatabase<GeneDef>.AllDefsListForReading.Where(g => Validate(g, geneSet, geneType, pawn))
                    .TryRandomElementByWeight(g => g.selectionWeight, out var gene))
                return;

            geneSet.AddGene(gene);
        }
    }

    public virtual bool Validate(GeneDef gene, GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        if (!geneSet.CanAddGeneDuringGeneration(gene))
            return false;

        if (gene.modContentPack != null && Config.Instance.ignoreGenesFromMods.Contains(gene.modContentPack.PackageId))
            return false;

        if (prohibitedGenes?.Contains(gene) is true)
            return false;

        if (gene.Extension_GeneWithComps is { } defExt)
        {
            if (defExt.gender != null && defExt.gender != pawn.gender)
                return false;
            if (defExt.geneType != null && defExt.geneType != geneType)
                return false;
        }

        // Aptitude-giving genes must not apply to only disabled skills
        if (!gene.aptitudes.NullOrEmpty() && gene.aptitudes.All(aptitude => pawn.skills.GetSkill(aptitude.skill).TotallyDisabled))
            return false;

        return true;
    }
}

public class GeneSetMakerWeight
{
    public GeneSetMaker maker;
    public float weight = 1f;
}

[UsedFromXml]
public class GeneSetMaker_Option : GeneSetMaker
{
    public override int BiostatMetForDisplay => Mathf.Clamp(0,
        options.Min(o => o.maker.BiostatMetForDisplay),
        options.Max(o => o.maker.BiostatMetForDisplay));

    public List<GeneSetMakerWeight> options;

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        options.RandomElementByWeight(o => o.weight).maker.AddGenes(geneSet, geneType, pawn);
    }
}

[UsedFromXml]
public class GeneSetMaker_Biostats : GeneSetMaker
{
    public override int BiostatMetForDisplay => Mathf.Clamp(0, biostatMet.min, biostatMet.max) * count.min;

    public IntRange biostatArc = IntRange.Zero;
    public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
    public IntRange biostatMet = new(int.MinValue, int.MaxValue);

    public override bool Validate(GeneDef gene, GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        if (!biostatMet.Includes(gene.biostatMet))
            return false;
        if (!biostatArc.Includes(gene.biostatArc))
            return false;
        if (!biostatCpx.Includes(gene.biostatCpx))
            return false;

        return base.Validate(gene, geneSet, geneType, pawn);
    }
}

[UsedFromXml]
public class GeneSetMaker_List : GeneSetMaker
{
    public override int BiostatMetForDisplay => biostatMetInternal ??= CalculateBiostatMet();

    private int? biostatMetInternal;

    private int CalculateBiostatMet()
    {
        if (count.min <= 0)
            return 0;

        List<int> metList = genes.Select(g => g.biostatMet).ToList();
        int minTotal = metList.OrderBy(m => m).Take(count.min).Sum();
        int maxTotal = metList.OrderByDescending(m => m).Take(count.min).Sum();
        return Mathf.Clamp(0, minTotal, maxTotal);
    }

    public List<GeneDef> genes;

    public static readonly List<GeneDef> genesTemp = [];

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        genesTemp.Clear();
        genesTemp.AddRange(genes);
        genesTemp.Shuffle();

        int added = 0;
        foreach (var gene in genesTemp)
        {
            if (Validate(gene, geneSet, geneType, pawn))
            {
                geneSet.AddGene(gene);
                added++;
                if (added >= countValue)
                    return;
            }
        }
    }
}
