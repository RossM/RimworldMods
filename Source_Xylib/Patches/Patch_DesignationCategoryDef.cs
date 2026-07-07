namespace Xylib.Patches;

[HarmonyPatch(typeof(DesignationCategoryDef))]
internal static class Patch_DesignationCategoryDef
{
    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
    public static void AllResolvedAndIdeoDesignators_Postfix(
        DesignationCategoryDef __instance,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ___ideoBuildingDesignatorsCached,
        ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result, ___ideoBuildingDesignatorsCached);
    }

    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
    public static void ResolvedAllowedDesignators_Postfix(
        DesignationCategoryDef __instance,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ___ideoBuildingDesignatorsCached,
        ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result, ___ideoBuildingDesignatorsCached);
    }
}
