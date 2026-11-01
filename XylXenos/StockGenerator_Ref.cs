using RimWorld.Planet;

namespace XylXenos;

[UsedFromXml]
public class StockGenerator_Ref : StockGenerator
{
    public required TraderKindDef traderKind;

    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction? faction = null) => 
        traderKind.stockGenerators.SelectMany(stockGenerator => stockGenerator.GenerateThings(forTile, faction));

    public override bool HandlesThingDef(ThingDef thingDef) => traderKind.stockGenerators.Any(s => s.HandlesThingDef(thingDef));
}
