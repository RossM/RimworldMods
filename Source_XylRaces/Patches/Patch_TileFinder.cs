using System;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(TileFinder))]
    public static class Patch_TileFinder
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(TileFinder.RandomSettlementTileFor),
             [typeof(PlanetLayer), typeof(Faction), typeof(bool), typeof(Predicate<PlanetTile>)])]
        public static void RandomSettlementTileFor_Prefix(PlanetLayer layer, Faction faction, bool mustBeAutoChoosable,
            ref Predicate<PlanetTile> extraValidator)
        {
            using (new ProfileBlock())
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
}
