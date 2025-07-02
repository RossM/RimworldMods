using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld.Planet;
using RimWorld;
using Verse;


namespace XylRacesCore
{
    [HarmonyPatch(typeof(RimWorld.Planet.TileFinder))]
    public static class Patch_TileFinder
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(RimWorld.Planet.TileFinder.RandomSettlementTileFor),
             [typeof(PlanetLayer), typeof(Faction), typeof(bool), typeof(Predicate<PlanetTile>)])]
        static void RandomSettlementTileFor_Prefix(PlanetLayer layer, Faction faction, bool mustBeAutoChoosable,
            ref Predicate<PlanetTile> extraValidator)
        {
            var extension = faction?.def?.GetModExtension<FactionDefExtension>();
            if (extension == null)
                return;

            var oldValidator = extraValidator;
            extraValidator = planetTile =>
            {
                if (oldValidator != null && !oldValidator(planetTile))
                    return false;

                return extension.ValidatePlanetTile(planetTile);
            };
        }
    }
}
