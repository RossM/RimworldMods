using RimWorld;
using RimWorld.Planet;

namespace XylXenos;

public static class MapHelpers
{
    public static bool IsCoastalOrRiverTile(this Tile tile)
    {
        return tile.IsCoastal || tile is SurfaceTile { Rivers.Count: > 0 };
    }

    public static bool IsWetlandBiome(this Tile tile)
    {
        return tile.PrimaryBiome == BiomeDefOf.TropicalSwamp || tile.PrimaryBiome == DefOf.TemperateSwamp ||
               tile.PrimaryBiome == BiomeDefOf.ColdBog;
    }
}
