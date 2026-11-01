namespace XylXenos;

[Patch(typeof(MeditationFocusDef))]
public static class Patch_MeditationFocusDef
{
    [Feature(nameof(GeneCompProperties_MeditationFocus))]
    [Postfix]
    [Target(nameof(MeditationFocusDef.EnablingThingsExplanation))]
    public static void EnablingThingsExplanation_Postfix(MeditationFocusDef __instance, Pawn pawn, ref string __result)
    {
        if (PatchHelpers.GenesEnableMeditationDef(pawn, __instance, out var gene))
            __result += $" - {"XylEnabledByGene".Translate()}: {gene.LabelCap}.";
    }
}
