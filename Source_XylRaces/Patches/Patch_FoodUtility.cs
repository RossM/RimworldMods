using static RimWorld.FoodUtility;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(FoodUtility))]
public static class Patch_FoodUtility
{
    [Feature(typeof(Hediff_DietDependency))]
    [Postfix]
    [Target(nameof(FoodOptimality))]
    public static void FoodOptimality_Postfix(
        Pawn eater,
        Thing foodSource,
        ref float __result)
    {
        __result += PatchHelpers.FoodOptimalityBonus(eater, foodSource);
    }

    [Feature(nameof(FoodHelpers.GetExtraNutritionFactor))]
    [Prefix]
    [Target("TryAddIngestThought")]
    public static bool TryAddIngestThought_Prefix(
        Pawn ingester,
        ThoughtDef def,
        ThingDef foodDef)
    {
        return !PatchHelpers.IsThoughtFromIngestionDisallowedByGenes(ingester, def, foodDef);
    }
}
