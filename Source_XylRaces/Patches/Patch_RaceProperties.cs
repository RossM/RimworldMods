namespace XylXenos.Patches;

[HarmonyPatch(typeof(RaceProperties))]
public static class Patch_RaceProperties
{
    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Prefix] [Inner(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef))]
    [Target(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
    public static bool GetFirstHediffOfDef_Prefix(HediffSet __instance, out Hediff? __result)
    {
        DebugAssert.NotNull(__instance.pawn);

        __result = null;
        // See comment in Patch_RaceProperties. There is a bug around lactation nutrition in the base game which causes
        // lactating pawns to need too much food. This turns out to be a problem for bossaps balance-wise, so I'm
        // fixing the bug.
        return !Settings.instance.ShouldFixLactationBugsFor(__instance.pawn);
    }

    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Prefix]
    [Target(nameof(RaceProperties.NutritionEatenPerDay))]
    private static bool NutritionEatenPerDay_Prefix(Pawn p, out string? __result)
    {
        __result = null;
        if (!Settings.instance.ShouldFixLactationBugsFor(p))
            return true;

        DebugAssert.NotNull(p.needs.food);

        // There is a bug in the base game that causes the nutrition from lactation to be counted twice, once as part of
        // NutritionEatenPerDay which is used to calculate food fall per tick, and then the lactation hediff itself also
        // directly consumes food per tick. This correctly displays that effect.
        float lactationNutritionUsed = p.LactationHediff?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;

        __result = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay + lactationNutritionUsed)
            .ToString("0.##");

        return false;
    }

    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Postfix] [InnerConstant("StatsReport_FinalValue")]
    [Target("NutritionEatenPerDayExplanation")]
    public static void StatsReport_FinalValue_Postfix([Parameter("p")] Pawn pawn, [State] StringBuilder sb)
    {
        PatchHelpers.AddLactationExplanation(sb, pawn);
    }

    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Postfix] [Inner(typeof(StringBuilder), memberType: MemberType.Constructor, parameterTypes: [])]
    [Target("NutritionEatenPerDayExplanation")]
    public static void StringBuilder_ctor_Postfix(StringBuilder __result, [State] out StringBuilder sb)
    {
        sb = __result;
    }
}
