namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class HediffCompProperties_Genetic : HediffCompPropertiesExt
{
    public GeneDef gene;
    public bool showStats = true;

    public HediffCompProperties_Genetic()
    {
        compClass = typeof(HediffComp_Genetic);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        return showStats && gene.Extension_GeneWithComps is { } geneWithComps ? geneWithComps.SpecialDisplayStats(request) : [];
    }

    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (gene is null)
            yield return "gene is null";
    }
}

[PublicAPI]
public class HediffComp_Genetic : HediffComp
{
    [NotNull] public HediffCompProperties_Genetic Props => (HediffCompProperties_Genetic)props;

    public override bool CompShouldRemove => !Pawn!.HasActiveGene(Props.gene!);
}
