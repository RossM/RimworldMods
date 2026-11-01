namespace XylIdeos;

[HarmonyPatch(typeof(Pawn_StyleTracker))]
public static class Patch_Pawn_StyleTracker
{
    [Feature(Features.PreventRestyleLoops)]
    [Postfix]
    [Target(nameof(Pawn_StyleTracker.HasUnwantedBeard), MemberType.Getter)]
    public static void HasUnwantedBeard_Postfix(Pawn_StyleTracker __instance, ref bool __result)
    {
        if (__result)
        {
            // Only want a style change if there are any valid options to change to
            __result = DefDatabase<BeardDef>.AllDefs.Any(beardDef => PawnStyleItemChooser.WantsToUseStyle(__instance.pawn, beardDef));
        }
    }

    [Feature(Features.PreventRestyleLoops)]
    [Postfix]
    [Target(nameof(Pawn_StyleTracker.HasUnwantedHairStyle), MemberType.Getter)]
    public static void HasUnwantedHairStyle_Postfix(Pawn_StyleTracker __instance, ref bool __result)
    {
        if (__result)
        {
            // Only want a style change if there are any valid options to change to
            __result = DefDatabase<HairDef>.AllDefs.Any(hairDef => PawnStyleItemChooser.WantsToUseStyle(__instance.pawn, hairDef));
        }
    }
}
