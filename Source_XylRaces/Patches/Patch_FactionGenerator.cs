using RimWorld.Planet;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(FactionGenerator))]
public static class Patch_FactionGenerator
{
    [Feature(nameof(Settings.useDistinctiveFactionColors))]
    [HarmonyPostfix]
    [HarmonyPatch("InitializeFactions")]
    public static void InitializeFactions_Postfix(PlanetLayer layer)
    {
        if (!Settings.instance.useDistinctiveFactionColors)
            return;

        PatchHelpers.ReassignFactionColors(layer);
    }
}