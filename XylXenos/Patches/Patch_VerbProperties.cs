namespace XylXenos.Patches;

[Patch(typeof(VerbProperties))]
public static class Patch_VerbProperties
{
    // The Verb overload delegates here, so patch only this overload to apply the factor once.
    [Feature(typeof(GeneCompProperties_MeleeDamageFactors))]
    [Postfix]
    [Target(nameof(VerbProperties.GetDamageFactorFor), typeof(Tool), typeof(Pawn), typeof(HediffComp_VerbGiver))]
    public static void GetDamageFactorFor_Postfix(VerbProperties __instance, Pawn? attacker, ref float __result)
    {
        __result *= PatchHelpers.GetDamageFactor(__instance, attacker);
    }

    [Feature(typeof(GeneCompProperties_MeleeDamageFactors))]
    [Postfix]
    [Inner(typeof(Tool), nameof(Tool.chanceFactor))]
    [Target(nameof(VerbProperties.AdjustedMeleeSelectionWeight), typeof(Tool), typeof(Pawn), typeof(Thing), typeof(HediffComp_VerbGiver),
        typeof(bool))]
    public static void Tool_chanceFactor_Postfix(VerbProperties __caller, Pawn? attacker, ref float __result)
    {
        __result += PatchHelpers.GetChanceBonus(__caller, attacker);
    }
}
