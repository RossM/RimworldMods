namespace XylXenos.Patches;

[Patch(typeof(VerbUtility))]
public static class Patch_VerbUtility
{
    [Feature(typeof(GeneCompProperties_MeleeDamageFactors))]
    [Postfix]
    [Inner(typeof(VerbUtility), "AdditionalSelectionFactor")]
    [Target(nameof(VerbUtility.InitialVerbWeight))]
    public static void AdditionalSelectionFactor_Postfix(Verb v, Pawn? p, ref float __result)
    {
        __result += PatchHelpers.GetChanceBonus(v.verbProps, p);
    }
}
