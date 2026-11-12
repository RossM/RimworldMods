namespace XylXenos;

[UsedFromXml]
public class ConditionalStatAffecter_Cold : ConditionalStatAffecter
{
    public float minTemperature = 11f;

    public override string Label => "XylStatReport_Cold".Translate(minTemperature.ToStringTemperature());

    public override bool Applies(StatRequest req) =>
        req.HasThing && req.Thing!.SpawnedOrAnyParentSpawned && req.Thing.AmbientTemperature < minTemperature;
}
