using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;

namespace XylRacesCore
{
    public class FactionDefExtension : DefModExtension
    {
        public List<BiomeDef> allowedBiomes;
        public List<Hilliness> allowedHilliness;
        public bool waterRequired = false;

        public bool ValidatePlanetTile(PlanetTile planetTile)
        {
            if (planetTile.Tile is not SurfaceTile surfaceTile)
                return false;

            if (waterRequired && !surfaceTile.IsCoastal && surfaceTile.Rivers is not { Count: > 0 }) 
                return false;

            if (allowedBiomes != null && !surfaceTile.Biomes.Any(biomeDef => allowedBiomes.Contains(biomeDef))) 
                return false;

            if (allowedHilliness != null && !allowedHilliness.Contains(surfaceTile.hilliness)) 
                return false;

            return true;
        }
    }
}
