using RimWorld.Planet;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(FactionGenerator))]
public static class Patch_FactionGenerator
{
    [Feature(nameof(Settings.useDistinctiveFactionColors))]
    [Postfix]
    [Target("InitializeFactions")]
    public static void InitializeFactions_Postfix(PlanetLayer layer)
    {
        if (!Settings.instance.useDistinctiveFactionColors)
            return;

        PatchHelpers.ReassignFactionColors(layer);
    }
}
