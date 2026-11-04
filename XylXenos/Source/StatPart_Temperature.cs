namespace XylXenos;

[UsedFromXml]
public class StatPart_Temperature : StatPart_Curve
{
    protected override bool AppliesTo(StatRequest req) => true;

    protected override float CurveXGetter(StatRequest req) => req.Thing!.AmbientTemperature;

    protected override string ExplanationLabel(StatRequest req) => "Temperature".Translate();
}
