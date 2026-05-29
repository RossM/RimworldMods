using RimWorld;
using RimWorld.Planet;

namespace XylXenos;

public static class MapHelpers
{
    extension(Tile tile)
    {
        public bool IsCoastalOrRiverTile()
        {
            return tile.IsCoastal || tile is SurfaceTile { Rivers.Count: > 0 };
        }

        public bool IsWetlandBiome()
        {
            return tile.PrimaryBiome == BiomeDefOf.TropicalSwamp || tile.PrimaryBiome == DefOf.TemperateSwamp ||
                   tile.PrimaryBiome == BiomeDefOf.ColdBog;
        }
    }
}
