namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnUtility))]
public static class Patch_PawnUtility
{
    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnDamageChanceFactor))]
    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnTameFailChanceFactor))]
    [InfixPostfix(typeof(Def), nameof(Def.LabelCap))]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
    public static void Def_LabelCap_Postfix(Def __instance, Pawn pawn, ref TaggedString __result)
    {
        if (__instance != pawn.def)
            return;

        var geneSet = pawn.GeneTracker;
        if (geneSet == null)
            return;

        if (geneSet.manhunterOnDamageChanceFactor != 1f || geneSet.manhunterOnTameFailChanceFactor != 1f)
            __result = pawn.genes.XenotypeLabelCap;
    }

    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnDamageChanceFactor))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(ThingDef)])]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(Pawn), typeof(Thing), typeof(float)])]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
    public static void GetManhunterOnDamageChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GeneTracker?.manhunterOnDamageChanceFactor ?? 1f;
    }

    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnTameFailChanceFactor))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(ThingDef)])]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(Pawn)])]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
    public static void GetManhunterOnTameFailChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GeneTracker?.manhunterOnTameFailChanceFactor ?? 1f;
    }

    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnDamageChanceFactor))]
    [InfixPostfix(typeof(RaceProperties), nameof(RaceProperties.manhunterOnDamageChance))]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
    public static void RaceProperties_manhunterOnDamageChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GeneTracker?.manhunterOnDamageChanceFactor ?? 1f;
    }

    [Feature(nameof(DefModExtension_GeneWithComps.manhunterOnTameFailChanceFactor))]
    [InfixPostfix(typeof(RaceProperties), nameof(RaceProperties.manhunterOnTameFailChance))]
    [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
    public static void RaceProperties_manhunterOnTameFailChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GeneTracker?.manhunterOnTameFailChanceFactor ?? 1f;
    }
}
