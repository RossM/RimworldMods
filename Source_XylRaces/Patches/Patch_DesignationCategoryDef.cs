namespace XylXenos.Patches;

[HarmonyPatch(typeof(DesignationCategoryDef))]
public static class Patch_DesignationCategoryDef
{
    [Feature(typeof(GeneComp_AddDesignators))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
    public static void AllResolvedAndIdeoDesignators_Postfix(
        DesignationCategoryDef __instance,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ___ideoBuildingDesignatorsCached,
        ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result, ___ideoBuildingDesignatorsCached);
    }

    [Feature(typeof(GeneComp_AddDesignators))]
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
