using RimWorld.Planet;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(TileFinder))]
public static class Patch_TileFinder
{
    [Feature(typeof(DefModExtension_Faction))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(TileFinder.RandomSettlementTileFor), typeof(PlanetLayer), typeof(Faction), typeof(bool),
        typeof(Predicate<PlanetTile>))]
    public static void RandomSettlementTileFor_Prefix(
        Faction faction,
        ref Predicate<PlanetTile>? extraValidator)
    {
        var extension = faction.def.GetModExtension<DefModExtension_Faction>();
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
