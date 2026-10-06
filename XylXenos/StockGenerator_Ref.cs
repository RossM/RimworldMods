using RimWorld.Planet;

namespace XylXenos;

[UsedFromXml]
public class StockGenerator_Ref : StockGenerator
{
    public required TraderKindDef traderKind;

    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction? faction = null)
    {
        foreach (var stockGenerator in traderKind.stockGenerators)
        foreach (var thing in stockGenerator.GenerateThings(forTile, faction))
            yield return thing;
    }

    public override bool HandlesThingDef(ThingDef thingDef)
    {
        return traderKind.stockGenerators.Any(s => s.HandlesThingDef(thingDef));
    }
}
