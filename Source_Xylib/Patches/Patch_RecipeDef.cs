namespace Xylib.Patches;

[HarmonyPatch(typeof(RecipeDef))]
internal static class Patch_RecipeDef
{
    [Feature(typeof(GeneCompProperties_UnlockRecipes))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
    public static void AvailableNow_Postfix(RecipeDef __instance, ref bool __result)
    {
        if (!Xylib.PatchHelpers.RecipesUnlockedByGenes.Contains(__instance))
            return;

        // If the recipe is available, and there was some non-research prerequisite that was met, it's available
        if (__result && (__instance.memePrerequisitesAny != null || __instance.factionPrerequisiteTags != null || __instance.fromIdeoBuildingPreceptOnly))
            return;

        // If the recipe has unmet research prerequisites, it's not available
        if (__instance.researchPrerequisite is { IsFinished: false })
            return;
        if (__instance.researchPrerequisites != null && __instance.researchPrerequisites.Any(r => !r.IsFinished))
            return;

        __result = Xylib.PatchHelpers.IsRecipeUnlockedByGenes(__instance);
    }
}
