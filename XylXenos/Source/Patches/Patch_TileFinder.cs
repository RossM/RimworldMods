using RimWorld.Planet;

namespace XylXenos.Patches;

[Patch(typeof(TileFinder))]
public static class Patch_TileFinder
{
    [Feature(typeof(DefModExtension_Faction))]
    [Prefix]
    [Target(nameof(TileFinder.RandomSettlementTileFor), typeof(PlanetLayer), typeof(Faction), typeof(bool),
        typeof(Predicate<PlanetTile>))]
    public static void RandomSettlementTileFor_Prefix(
        Faction? faction,
        ref Predicate<PlanetTile>? extraValidator)
    {
        if (faction?.def.GetModExtension<DefModExtension_Faction>() is not { } extension)
            return;

        var oldValidator = extraValidator;
        extraValidator = planetTile =>
        {
            if (oldValidator is not null && !oldValidator(planetTile))
                return false;

            return extension.ValidatePlanetTile(planetTile);
        };
    }
}
