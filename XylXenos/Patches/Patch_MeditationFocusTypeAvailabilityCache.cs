namespace XylXenos.Patches;

[Patch(typeof(MeditationFocusTypeAvailabilityCache))]
public static class Patch_MeditationFocusTypeAvailabilityCache
{
    [Feature(nameof(GeneCompProperties_MeditationFocus))]
    [Postfix]
    [Target("PawnCanUseInt")]
    public static void PawnCanUseInt_Postfix(Pawn p, MeditationFocusDef type, ref bool __result)
    {
        if (__result)
            return;

        if (PatchHelpers.GenesEnableMeditationDef(p, type, out _))
            __result = true;
    }
}
