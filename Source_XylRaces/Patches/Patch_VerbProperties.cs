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
        if (__instance is not { IsMeleeAttack: true, meleeDamageDef: { } damageDef })
            return;

        if (attacker?.GeneTracker_XylXenos?.meleeDamageFactors is { } factors &&
            factors.TryGetValue(damageDef, out float factor))
            __result *= factor;
    }
}
