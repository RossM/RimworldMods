namespace XylXenos;

public abstract class GeneSetMaker
{
    public IntRange count = IntRange.One;
    public List<GeneDef> prohibitedGenes;

    public virtual int BiostatMetForDisplayBonus => 0;

    public virtual GeneSet Generate(Pawn pawn, GeneType geneType = GeneType.Xenogene)
    {
        var geneSet = new GeneSet();

        foreach (var gene in pawn.genes.GenesListForReading.Where(g => g.Active))
            geneSet.AddGene(gene.def);

        int existingGeneCount = geneSet.GenesListForReading.Count;

        AddGenes(geneSet, geneType, pawn);

        var newGenes = new GeneSet();
        for (int i = existingGeneCount; i < geneSet.GenesListForReading.Count; i++)
            newGenes.AddGene(geneSet.GenesListForReading[i]);

        return newGenes;
    }

    public virtual void AddGenes(GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        int countValue = count.RandomInRange;

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

        foreach (var otherGene in geneSet.GenesListForReading)
        {
            if (gene.exclusionTags != null && otherGene.exclusionTags != null &&
                gene.exclusionTags.Intersect(otherGene.exclusionTags).Any())
            {
                return false;
            }
        }

        return true;
    }
}

public struct GeneSetMakerWeight
{
    public GeneSetMaker maker;
    public float weight;
}

[UsedFromXml]
public class GeneSetMaker_Option : GeneSetMaker
{
    public List<GeneSetMakerWeight> options;

    public override void AddGenes(GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        options.RandomElementByWeight(o => o.weight).maker.AddGenes(geneSet, geneType, pawn);
    }
}

[UsedFromXml]
public class GeneSetMaker_Biostats : GeneSetMaker
{
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
    public List<GeneDef> genes;

    public override int BiostatMetForDisplayBonus => genes.Min(g => g.biostatMet);

    public override void AddGenes(GeneSet geneSet, GeneType geneType, Pawn pawn)
    {
        int countValue = count.RandomInRange;

        for (int i = 0; i < countValue; i++)
        {
            if (!genes.Where(g => Validate(g, geneSet, geneType, pawn)).TryRandomElement(out var gene))
                return;

            geneSet.AddGene(gene);
        }
    }
}
