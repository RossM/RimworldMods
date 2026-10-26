namespace Xylib.Patches;

[Patch(typeof(RecipeDef))]
internal static class Patch_RecipeDef
{
    [Feature(typeof(GeneCompProperties_UnlockRecipes))]
    [Postfix]
    [Inner(typeof(RecipeDef), nameof(RecipeDef.memePrerequisitesAny))]
    [Target(nameof(RecipeDef.AvailableNow), MemberType.Getter)]
    public static void RecipeDef_memePrerequisitesAny_Postfix(RecipeDef __instance, ref List<MemeDef>? __result)
    {
        if (!PatchHelpers.RecipesUnlockableByGenes.Contains(__instance))
            return;

        // If a gene unlocks the recipe, skip the meme prerequisite check and make it available; otherwise,
        // require the original meme prerequisite (if any) or an empty list to make it unavailable.
        if (PatchHelpers.IsRecipeUnlockedByGenes(__instance))
            __result = null;
        else if (__result == null)
            __result = [];
    }
}
