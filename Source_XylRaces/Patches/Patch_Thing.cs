namespace XylXenos.Patches;

[HarmonyPatch(typeof(Thing))]
public static class Patch_Thing
{
    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thing.Destroy))]
    public static void Destroy_Postfix(Thing __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            EventManager.Instance.UnregisterAll(target);
    }

    [Feature(nameof(EventDefOf.PostDiscard))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thing.Discard))]
    public static void Discard_Postfix(Thing __instance)
    {
        if (!IsInteresting(__instance))
            return;

        EventManager.Instance.Notify(EventDefOf.PostDiscard, __instance);
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

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thing.PostMake))]
    public static void PostMake_Postfix(Thing __instance)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (__instance is IEventListener target)
            target.RegisterWith(EventManager.Instance);

        if (!IsInteresting(__instance))
            return;

        EventManager.Instance.Notify(EventDefOf.PostPostMake, __instance);
    }

    [Feature(nameof(EventDefOf.PreTakeDamage))]
    [HarmonyPrefix]
    [HarmonyPatch("TakeDamage")]
    public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo)
    {
        if (!IsInteresting(__instance))
            return;

        EventManager.Instance.Notify(EventDefOf.PreTakeDamage, __instance, dinfo);
    }

    private static bool IsInteresting(Thing thing) => thing is ThingWithComps and not (Projectile or Plant);
}
