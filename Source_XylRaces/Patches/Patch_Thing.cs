namespace XylXenos.Patches;

[HarmonyPatch(typeof(Thing))]
public static class Patch_Thing
{
    [Feature(typeof(Hediff_DietDependency))]
    [Prefix]
    [Target("IngestedCalculateAmounts")]
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
