namespace Xylib.Patches;

[Patch(typeof(PawnUtility))]
internal static class Patch_PawnUtility
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Postfix]
    [Target(nameof(PawnUtility.CanTakeDrug))]
    public static void CanTakeDrug_Postfix(Pawn pawn, ThingDef drug, ref bool __result)
    {
        if (!pawn.ChemicalIsAllowedByGenes(drug))
            __result = false;
    }


    [Feature(nameof(XStatDefOf.XylManhunterChanceOnDamageFactor))]
    [Postfix]
    [Target(nameof(PawnUtility.GetManhunterOnDamageChance), typeof(Pawn), typeof(Thing), typeof(float))]
    public static void GetManhunterOnDamageChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GetStatValue(XStatDefOf.XylManhunterChanceOnDamageFactor);
    }

    [Feature(nameof(XStatDefOf.XylManhunterChanceOnTameFailFactor))]
    [Postfix]
    [Target(nameof(PawnUtility.GetManhunterOnTameFailChance), typeof(Pawn))]
    public static void GetManhunterOnTameFailChance_Postfix(Pawn pawn, ref float __result)
    {
        __result *= pawn.GetStatValue(XStatDefOf.XylManhunterChanceOnTameFailFactor);
    }

    [Feature(nameof(XStatDefOf.XylManhunterChanceOnDamageFactor))]
    [Postfix]
    [Inner(typeof(StringBuilder), memberType: MemberType.Constructor, parameterTypes: [])]
    [Target(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
    [Target(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
    public static void StringBuilder_constructor_Postfix(ref StringBuilder __result, [State] out StringBuilder sb)
    {
        sb = __result;
    }

    [Feature(nameof(XStatDefOf.XylManhunterChanceOnTameFailFactor))]
    [Postfix]
    [InnerConstant("StatsReport_FinalValue")]
    [Target(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
    public static void GetManhunterOnTameFailChanceExplanation_StatsReport_FinalValue_Postfix(Pawn pawn, [State] StringBuilder sb)
    {
        PatchHelpers.AppendExtraStatFactors(pawn, XStatDefOf.XylManhunterChanceOnTameFailFactor, sb);
    }

    [Feature(nameof(XStatDefOf.XylManhunterChanceOnDamageFactor))]
    [Postfix]
    [InnerConstant("StatsReport_FinalValue")]
    [Target(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
    public static void GetManhunterOnDamageChanceExplanation_StatsReport_FinalValue_Postfix(Pawn pawn, [State] StringBuilder sb)
    {
        PatchHelpers.AppendExtraStatFactors(pawn, XStatDefOf.XylManhunterChanceOnDamageFactor, sb);
    }
}
