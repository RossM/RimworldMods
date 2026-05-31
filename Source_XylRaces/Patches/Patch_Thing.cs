namespace XylXenos.Patches;

[HarmonyPatch(typeof(Thing))]
public static class Patch_Thing
{
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

    [Feature(nameof(NotificationDefOf.PreDamageTaken))]
    [HarmonyPrefix]
    [HarmonyPatch("TakeDamage")]
    public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PreDamageTaken, __instance, dinfo);
    }

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

    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thing.PostMake))]
    public static void PostMake_Postfix(Thing __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is INotificationListener target)
            target.RegisterWith(NotificationManager.Instance);
    }

    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thing.Destroy))]
    public static void Destroy_Postfix(Thing __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is INotificationListener target)
            NotificationManager.Instance.UnregisterAll(target);
    }
}
