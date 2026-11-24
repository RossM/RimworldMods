namespace XylXenos;

[UsedFromXml]
public class ConditionalStatAffecter_Hot : ConditionalStatAffecter
{
    public override string Label => "XylStatReport_Hot".Translate(maxTemperature.ToStringTemperature());
    private float maxTemperature = 31f;

    public override bool Applies(StatRequest req) =>
        req.HasThing && req.Thing!.SpawnedOrAnyParentSpawned && req.Thing.AmbientTemperature > maxTemperature;
}
