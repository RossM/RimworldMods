using RimWorld.Planet;

namespace XylXenos;

public static class TileExtensions
{
    extension(Tile tile)
    {
        public bool IsCoastalOrRiverTile => tile.IsCoastal || tile is SurfaceTile { Rivers.Count: > 0 };

        public bool IsWetlandBiome =>
            tile.PrimaryBiome == BiomeDefOf.TropicalSwamp ||
            tile.PrimaryBiome == DefOf.TemperateSwamp ||
            tile.PrimaryBiome == BiomeDefOf.ColdBog;
    }
}
