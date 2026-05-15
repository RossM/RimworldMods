using RimWorld.Planet;

namespace XylRacesCore;

public static class MapHelpers
{
    public static bool IsCoastalOrRiverTile(this Tile tile)
    {
        return tile.IsCoastal || tile is SurfaceTile { Rivers.Count: > 0 };
    }
}
