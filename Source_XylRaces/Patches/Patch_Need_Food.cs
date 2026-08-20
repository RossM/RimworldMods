namespace XylXenos.Patches;

[HarmonyPatch(typeof(Need_Food))]
public static class Patch_Need_Food
{
    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Postfix]
    [Inner(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay))]
    [Target("FoodFallPerTickAssumingCategory")]
    private static void AddedNutritionPerDay_Postfix(HediffComp_Lactating __instance, ref float __result)
    {
        DebugAssert.NotNull(__instance.Pawn);

        if (Settings.instance.ShouldFixLactationBugsFor(__instance.Pawn))
            __result = 0f;
    }
}
