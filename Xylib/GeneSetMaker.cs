namespace Xylib;

/// <summary>
///     This class configures a gene set maker that can generate different, randomized gene sets during gameplay.
/// </summary>
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
            foreach (var gene in pawn.genes.Xenotype.genes)
                geneSet.AddGene(gene);

        if (pawn.genes.CustomXenotype != null)
            foreach (var gene in pawn.genes.CustomXenotype.genes)
                geneSet.AddGene(gene);

        foreach (var gene in pawn.genes.GenesListForReading.Where(g => g.Active))
            geneSet.AddGene(gene.def);

        int existingGeneCount = genes.Count;

        AddGenes(geneSet, geneType, pawn);

        var newGenes = new GeneSet();
        for (int i = existingGeneCount; i < genes.Count; i++)
        {
            var gene = genes[i];
            DebugAssert.NotNull(gene);

            newGenes.AddGene(gene);
        }

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
        if (gene.aptitudes is { Count: > 0 } &&
            gene.aptitudes.All(aptitude => pawn.skills?.GetSkill(aptitude.skill)?.TotallyDisabled is not false))
            return false;

        return true;
    }

    public virtual IEnumerable<string> ConfigErrors()
    {
        return PatchHelpers.RequiredMemberErrors(this) ?? [];
    }

    public virtual void ResolveReferences() { }
}

[PublicAPI]
public class GeneSetMakerWeight
{
    public required GeneSetMaker maker;
    public float weight = 1f;
}

/// <summary>
///     Generates genes by randomly selecting from several options that are each themselves a <see cref="GeneSetMaker" />.
/// </summary>
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
            options.RandomElementByWeight(o => o.weight).maker.AddGenes(geneSet, geneType, pawn);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (options is null)
            yield break;

        foreach (var option in options)
        {
            if (option.maker == null)
                yield return $"null {nameof(option.maker)} in {nameof(option)}";
            else
                foreach (var error in option.maker.ConfigErrors())
                    yield return error;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        foreach (var option in options)
            option.maker.ResolveReferences();
    }
}

/// <summary>
///     Generates genes by referencing a <see cref="GeneSetMakerDef" />.
/// </summary>
[UsedFromXml]
[PublicAPI]
public class GeneSetMaker_Subtree : GeneSetMaker
{
    public override int BiostatMetForDisplay => def.root.BiostatMetForDisplay;

    public required GeneSetMakerDef def;

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        def.root.AddGenes(geneSet, geneType, pawn);
    }
}

/// <summary>
///     Generates genes randomly selected from the entire gene list, filtered by biostats.
/// </summary>
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

/// <summary>
///     Generates genes by selecting random genes from a list of specific <see cref="GeneDef" />s.
/// </summary>
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

        List<int> metList = [.. genes.Select(g => g.biostatMet)];
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
}

/// <summary>
///     Generates genes by calling a list of <see cref="GeneSetMaker" />s then accepting or rejecting the result based on total biostats.
/// </summary>
public class GeneSetMaker_BiostatTotal : GeneSetMaker
{
    private const int TryCount = 100;

    public override int BiostatMetForDisplay => Mathf.Clamp(0, biostatMet.min, biostatMet.max);

    public IntRange biostatArc = IntRange.Zero;
    public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
    public IntRange biostatMet = new(int.MinValue, int.MaxValue);
    public required List<GeneSetMaker> subMakers;
    public bool shuffle = true;

    private List<GeneSetMaker>? subMakersTemp;

    protected override void AddGenesInt(GeneSet geneSet, GeneType geneType, Pawn pawn, int countValue)
    {
        subMakersTemp ??= [.. subMakers];

        // "For reading" is just a suggestion
        List<GeneDef> genes = geneSet.GenesListForReading;
        
        int initialGeneCount = genes.Count;

        for (int iteration = 0; iteration < TryCount; iteration++)
        {
            genes.RemoveRange(initialGeneCount, genes.Count);
            if (shuffle)
                subMakersTemp.Shuffle();

            for (int i = 0; i < countValue && i < subMakers.Count; i++)
                subMakers[i].AddGenes(geneSet, geneType, pawn);

            int totalBiostatArc = 0;
            int totalBiostatCpx = 0;
            int totalBiostatMet = 0;
            for (int i = initialGeneCount; i < genes.Count; i++)
            {
                totalBiostatArc += genes[i].biostatArc;
                totalBiostatCpx += genes[i].biostatCpx;
                totalBiostatMet += genes[i].biostatMet;
            }

            if (biostatArc.Includes(totalBiostatArc) && biostatCpx.Includes(totalBiostatCpx) && biostatMet.Includes(totalBiostatMet))
                return;
        }

        Log.WarningOnce($"GeneSetMaker_BiostatTotal failed to generate a valid gene set within {TryCount} attempts, using last result", 0x2AE4C1BA);
    }
}