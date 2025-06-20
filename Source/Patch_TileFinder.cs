using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(RimWorld.Planet.TileFinder), nameof(RimWorld.Planet.TileFinder.RandomSettlementTileFor),
        [typeof(PlanetLayer), typeof(Faction), typeof(bool), typeof(Predicate<PlanetTile>)])]
    public class Patch_TileFinder
    {
        [HarmonyPrefix]
        static void Prefix(PlanetLayer layer, Faction faction, bool mustBeAutoChoosable, ref Predicate<PlanetTile> extraValidator)
        {
            var extension = faction?.def?.GetModExtension<ModExtension_FactionDef>();
            if (extension == null)
                return;

            if (extension.waterRequired)
            {
                var oldValidator = extraValidator;
                extraValidator = planetTile =>
                {
                    if (oldValidator != null && !oldValidator(planetTile))
                        return false;
                    if (planetTile.Tile is not SurfaceTile surfaceTile) 
                        return false;
                    //Log.Message(string.Format("surfaceTile: {0}", surfaceTile));
                    return surfaceTile.IsCoastal || surfaceTile.Rivers is { Count: > 0 };
                };
            }

            if (extension.allowedBiomes != null)
            {
                var oldValidator = extraValidator;
                extraValidator = planetTile =>
                {
                    if (oldValidator != null && !oldValidator(planetTile))
                        return false;
                    if (planetTile.Tile is not SurfaceTile surfaceTile) 
                        return false;
                    return surfaceTile.Biomes.Any(biomeDef => extension.allowedBiomes.Contains(biomeDef));
                };
            }

            if (extension.allowedHilliness != null)
            {
                var oldValidator = extraValidator;
                extraValidator = planetTile =>
                {
                    if (oldValidator != null && !oldValidator(planetTile))
                        return false;
                    if (planetTile.Tile is not SurfaceTile surfaceTile)
                        return false;
                    return extension.allowedHilliness.Contains(surfaceTile.hilliness);
                };
            }
        }
    }
}
