namespace XylXenos.Patches;

[HarmonyPatch(typeof(DesignationCategoryDef))]
public static class Patch_DesignationCategoryDef
{
    [Feature(nameof(DefExt.addDesignators))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
    public static void ResolvedAllowedDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result);
    }

    [Feature(nameof(DefExt.addDesignators))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
    public static void AllResolvedAndIdeoDesignators_Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        PatchHelpers.AddDesignators(__instance, ref __result);
    }
}