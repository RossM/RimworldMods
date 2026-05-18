using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XylXenos
{
    public class FactionDefExtension : DefModExtension
    {
        public List<BiomeDef> allowedBiomes;
        public List<Hilliness> allowedHilliness;
        public bool waterRequired = false;
        public FloatRange? nearbyPollution;

        public bool ValidatePlanetTile(PlanetTile planetTile)
        {
            if (planetTile.Tile is not SurfaceTile surfaceTile)
                return false;

            if (waterRequired && !surfaceTile.IsCoastalOrRiverTile())
                return false;

            if (allowedBiomes != null && !surfaceTile.Biomes.Any(biomeDef => allowedBiomes.Contains(biomeDef)))
                return false;

            if (allowedHilliness != null && !allowedHilliness.Contains(surfaceTile.hilliness))
                return false;

            if (nearbyPollution != null &&
                !nearbyPollution.Value.Includes(WorldPollutionUtility.CalculateNearbyPollutionScore(planetTile)))
                return false;

            return true;
        }
    }
}
