namespace XylXenos;

[UsedFromXml]
public class ConditionalStatAffecter_Cold : ConditionalStatAffecter
{
    public override string Label => "XylStatReport_Cold".Translate(minTemperature.ToStringTemperature());
    public float minTemperature = 11f;

    public override bool Applies(StatRequest req) =>
        req.HasThing && req.Thing!.SpawnedOrAnyParentSpawned && req.Thing.AmbientTemperature < minTemperature;
}
