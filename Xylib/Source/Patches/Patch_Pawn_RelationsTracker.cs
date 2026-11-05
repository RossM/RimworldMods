namespace Xylib.Patches;

[Patch(typeof(Pawn_RelationsTracker))]
internal static class Patch_Pawn_RelationsTracker
{
    [Feature(nameof(XStatDefOf.XylRomanceChanceFactor))]
    [Postfix]
    [Target(nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
    public static void SecondaryRomanceChanceFactor_Postfix(Pawn otherPawn, ref float __result)
    {
        __result *= otherPawn.GetStatValue(XStatDefOf.XylRomanceChanceFactor);
    }
}
