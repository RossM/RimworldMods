using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(TileFinder))]
    public static class Patch_TileFinder
    {
        [Feature(typeof(FactionDefExtension))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TileFinder.RandomSettlementTileFor), typeof(PlanetLayer), typeof(Faction), typeof(bool),
            typeof(Predicate<PlanetTile>))]
        public static void RandomSettlementTileFor_Prefix(
            PlanetLayer layer,
            Faction faction,
            bool mustBeAutoChoosable,
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
