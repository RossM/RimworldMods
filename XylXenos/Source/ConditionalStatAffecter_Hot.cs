namespace XylXenos;

[UsedFromXml]
public class ConditionalStatAffecter_Hot : ConditionalStatAffecter
{
    private float maxTemperature = 31f;

    public override string Label => "XylStatReport_Hot".Translate(maxTemperature.ToStringTemperature());

    public override bool Applies(StatRequest req) =>
        req.HasThing && req.Thing!.SpawnedOrAnyParentSpawned && req.Thing.AmbientTemperature > maxTemperature;
}
