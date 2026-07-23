namespace XylXenos.Patches;

[HarmonyPatch]
public static class PatchLactation
{
    [Feature(typeof(GeneComp_Hyperlactation))]
    [InnerPostfix(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), typeof(HediffDef), typeof(bool))]
    [Target(typeof(ChildcareUtility), "CanBreastfeedNow")]
    [Target(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
    [Target(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
    [Target(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
    [Target(typeof(ITab_Pawn_Feeding), "DrawRow")]
    public static void GetFirstHediffOfDef_Postfix(HediffSet __instance, HediffDef def, bool mustBeVisible, ref Hediff __result)
    {
        DebugAssert.NotNull(__instance.pawn);

        if (def == HediffDefOf.Lactating && !mustBeVisible)
            __result = __instance.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
    }

    [Feature(typeof(GeneComp_Hyperlactation))]
    [InnerPostfix(typeof(HediffSet), nameof(HediffSet.HasHediff), typeof(HediffDef), typeof(bool))]
    [Target(typeof(ChildcareUtility), "CanBreastfeed")]
    public static void HasHediff_Postfix(HediffSet __instance, HediffDef def, bool mustBeVisible, ref bool __result)
    {
        DebugAssert.NotNull(__instance.pawn);

        if (def == HediffDefOf.Lactating && !mustBeVisible)
            __result = __instance.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
    }
}
