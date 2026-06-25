namespace XylXenos.Patches;

[HarmonyPatch(typeof(Thing))]
public static class Patch_Thing
{
    [Feature(nameof(FoodHelpers.GetFoodPoisonChanceFactor))]
    [InfixPostfix(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    [InfixPatch("Ingested")]
    public static void GetStatValue_Postfix(Pawn ingester, Thing thing, StatDef stat, ref float __result)
    {
        if (stat == StatDefOf.FoodPoisonChanceFixedHuman)
        {
            __result *= FoodHelpers.GetFoodPoisonChanceFactor(ingester, thing);
        }
    }

    [Feature(typeof(Hediff_DietDependency))]
    [HarmonyPrefix]
    [HarmonyPatch("IngestedCalculateAmounts")]
    public static void IngestedCalculateAmounts_Prefix(Thing __instance, Pawn ingester, ref float nutritionWanted)
    {
        foreach (var hediff in ingester.HediffsOfType<Hediff_DietDependency>())
        {
            if (!hediff.ValidateFood(__instance))
                continue;

            nutritionWanted = Math.Max(nutritionWanted, hediff.NutritionWantedToSatisfy());
        }
    }
}
