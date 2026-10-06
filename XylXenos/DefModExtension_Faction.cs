using RimWorld.Planet;

namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Faction : DefModExtension
{
    public List<BiomeDef>? allowedBiomes;
    public List<Hilliness>? allowedHilliness;
    public bool waterRequired = false;
    public FloatRange? nearbyPollution;

    public bool ValidatePlanetTile(PlanetTile planetTile)
    {
        if (planetTile.Tile is not SurfaceTile surfaceTile)
            return false;

        if (allowedBiomes is not { Count: > 0 } && allowedHilliness is not { Count: > 0 } && !waterRequired && !nearbyPollution.HasValue)
            return true;

        if (waterRequired && surfaceTile.IsCoastalOrRiverTile)
            return true;

        if (allowedBiomes is { Count: > 0 } && surfaceTile.Biomes.Any(biomeDef => allowedBiomes.Contains(biomeDef)))
            return true;

        if (allowedHilliness is { Count: > 0 } && allowedHilliness.Contains(surfaceTile.hilliness))
            return true;

        if (nearbyPollution?.Includes(WorldPollutionUtility.CalculateNearbyPollutionScore(planetTile)) ?? false)
            return true;

        return false;
    }
}
