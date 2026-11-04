namespace XylXenos;

[UsedFromXml]
public class StatPart_Temperature : StatPart_Curve
{
    public List<GeneDef>? showForPawnsWithGenes;

    protected override bool AppliesTo(StatRequest req) => req.Thing is Pawn;

    protected override float CurveXGetter(StatRequest req) => req.Thing!.AmbientTemperature;

    protected override string ExplanationLabel(StatRequest req) => "Temperature".Translate();

    public override bool ForceShow(StatRequest req) => showForPawnsWithGenes is { Count: >0 } && req.Thing is Pawn p && showForPawnsWithGenes.Any(g => p.HasActiveGene(g));
}
