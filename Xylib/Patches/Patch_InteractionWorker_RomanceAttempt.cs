namespace Xylib.Patches;

[Patch(typeof(InteractionWorker_RomanceAttempt))]
public static class Patch_InteractionWorker_RomanceAttempt
{
    [Feature(nameof(XStatDefOf.XylRomanceChanceFactor))]
    [Postfix]
    [Target(nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight))]
    public static void RandomSelectionWeight_Postfix(Pawn initiator, Pawn recipient, ref float __result)
    {
        __result *= initiator.GetStatValue(XStatDefOf.XylRomanceChanceFactor);
    }
}
