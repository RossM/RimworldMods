namespace Xylib;

[UsedFromXml]
[PublicAPI]
public abstract class GeneSetMaker
{
    public virtual int BiostatMetForDisplay => 0;

    public float chance = 1f;
    public IntRange count = IntRange.One;

    public GeneSet Generate(Pawn pawn, GeneType geneType = GeneType.Xenogene)
    {
        if (pawn.genes == null)
            throw new ArgumentException("genes is null", nameof(pawn));

        var geneSet = new GeneSet();
        List<GeneDef> genes = geneSet.GenesListForReading;

        if (pawn.genes.Xenotype != null)
        {
            foreach (var gene in pawn.genes.Xenotype.genes!)
                geneSet.AddGene(gene);
        }

        if (pawn.genes.CustomXenotype != null)
        {
            foreach (var gene in pawn.genes.CustomXenotype.genes!)
                geneSet.AddGene(gene);
        }

        foreach (var gene in pawn.genes.GenesListForReading.Where(g => g.Active))
            geneSet.AddGene(gene.def);

        int existingGeneCount = genes.Count;

        AddGenes(geneSet, geneType, pawn);

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

        if (gene.Extension_GeneWithComps?.ValidFor(pawn, geneType) is false)
            return false;

        // Aptitude-giving genes must not apply to only disabled skills
        if (gene.aptitudes is { Count: > 0 } && gene.aptitudes.All(aptitude => pawn.skills?.GetSkill(aptitude.skill)?.TotallyDisabled is not false))
            return false;

        return true;
    }

    public virtual IEnumerable<string> ConfigErrors()
    {
        return [];
    }

    public virtual void ResolveReferences()
    {
    }
}

[PublicAPI]
public class GeneSetMakerWeight
{
    public required GeneSetMaker maker;
    public float weight = 1f;
}

[UsedFromXml]
[PublicAPI]
public class GeneSetMaker_Option : GeneSetMaker
{
    public override int BiostatMetForDisplay =>
        count.min * Mathf.Clamp(0,
            options.Min(o => o.maker.BiostatMetForDisplay),
            options.Max(o => o.maker.BiostatMetForDisplay));

    public required List<GeneSetMakerWeight> options;

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        for (int i = 0; i < countValue; i++)
            options.RandomElementByWeight(o => o!.weight)!.maker.AddGenes(geneSet, geneType, pawn);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (options is null)
        {
            yield return $"{nameof(options)} is null";
            yield break;
        }

        foreach (var option in options)
        {
            if (option.maker == null)
                yield return $"null {nameof(option.maker)} in {nameof(option)}";
            else
            {
                foreach (var error in option.maker.ConfigErrors())
                    yield return error;
            }
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        foreach (var option in options)
            option.maker.ResolveReferences();
    }
}

[UsedFromXml]
[PublicAPI]
public class GeneSetMaker_Subtree : GeneSetMaker
{
    public required GeneSetMakerDef def;

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        def.root.AddGenes(geneSet, geneType, pawn);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (def is null)
            yield return $"{nameof(def)} is null";
    }
}

[UsedFromXml]
[PublicAPI]
public class GeneSetMaker_Biostats : GeneSetMaker
{
    public override int BiostatMetForDisplay => Mathf.Clamp(0, biostatMet.min, biostatMet.max) * count.min;

    public IntRange biostatArc = IntRange.Zero;
    public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
    public IntRange biostatMet = new(int.MinValue, int.MaxValue);

    public List<GeneDef>? prohibitedGenes;
    [NoTranslate] public List<string?>? prohibitedModContentPacks;

    public override bool Validate(GeneDef gene, GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        if (prohibitedModContentPacks?.Contains(gene.modContentPack?.PackageId) is true)
            return false;
        if (prohibitedGenes?.Contains(gene) is true)
            return false;

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
[PublicAPI]
public class GeneSetMaker_List : GeneSetMaker
{
    public override int BiostatMetForDisplay => biostatMetInternal ??= CalculateBiostatMet();

    public static readonly List<GeneDef> genesTemp = [];

    private int? biostatMetInternal;

    public required List<GeneDef> genes;

    private int CalculateBiostatMet()
    {
        if (count.min <= 0)
            return 0;

        if (genes == null)
            throw new InvalidOperationException();

        List<int> metList = genes.Select(g => g.biostatMet).ToList();
        int minTotal = metList.OrderBy(m => m).Take(count.min).Sum();
        int maxTotal = metList.OrderByDescending(m => m).Take(count.min).Sum();
        return Mathf.Clamp(0, minTotal, maxTotal);
    }

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        if (genes == null)
            throw new InvalidOperationException();

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

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (genes is null)
            yield return $"{nameof(genes)} is null";
    }
}
