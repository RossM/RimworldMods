namespace XylXenos;

[UsedFromXml]
public class StatPart_Temperature : StatPart_Curve
{
    public List<GeneDef>? showForPawnsWithGenes;

    protected override bool AppliesTo(StatRequest req) => req.Thing is Pawn;

    // The base stat part evaluates the curve only when AppliesTo confirms the request contains a pawn.
    protected override float CurveXGetter(StatRequest req) => req.Thing!.AmbientTemperature;

    protected override string ExplanationLabel(StatRequest req) => "Temperature".Translate();

    public override bool ForceShow(StatRequest req) => showForPawnsWithGenes is { Count: > 0 } && req.Thing is Pawn p &&
                                                       showForPawnsWithGenes.Any(g => p.HasActiveGene(g));
}
