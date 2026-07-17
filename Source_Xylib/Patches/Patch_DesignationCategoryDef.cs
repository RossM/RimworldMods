namespace Xylib.Patches;

[HarmonyPatch(typeof(DesignationCategoryDef))]
internal static class Patch_DesignationCategoryDef
{
    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [Postfix]
    [Target(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MemberType.Getter)]
    public static void AllResolvedAndIdeoDesignators_Postfix(
        DesignationCategoryDef __instance,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ___ideoBuildingDesignatorsCached,
        ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result, ___ideoBuildingDesignatorsCached);
    }

    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [Postfix]
    [Target(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MemberType.Getter)]
    public static void ResolvedAllowedDesignators_Postfix(
        DesignationCategoryDef __instance,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ___ideoBuildingDesignatorsCached,
        ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result, ___ideoBuildingDesignatorsCached);
    }
}
