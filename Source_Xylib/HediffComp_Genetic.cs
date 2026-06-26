namespace Xylib;

[UsedFromXml]
public class HediffCompProperties_Genetic : HediffCompProperties
{
    public GeneDef gene;

    public HediffCompProperties_Genetic()
    {
        compClass = typeof(HediffComp_Genetic);
    }
}

public class HediffComp_Genetic : HediffComp
{
    public HediffCompProperties_Genetic Props => (HediffCompProperties_Genetic)props;

    public override bool CompShouldRemove => !Pawn.HasActiveGene(Props.gene);
}
