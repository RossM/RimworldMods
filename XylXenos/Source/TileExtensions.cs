using RimWorld.Planet;

namespace XylXenos;

public static class TileExtensions
{
    extension(Tile tile)
    {
        public bool IsCoastalOrRiverTile => tile is { IsCoastal: true } or SurfaceTile { Rivers.Count: > 0 };

        public bool IsWetlandBiome =>
            tile.PrimaryBiome == BiomeDefOf.TropicalSwamp ||
            tile.PrimaryBiome == VanillaDefOf.TemperateSwamp ||
            tile.PrimaryBiome == BiomeDefOf.ColdBog;
    }
}
