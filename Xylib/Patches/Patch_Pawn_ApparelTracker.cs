namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_ApparelTracker))]
public static class Patch_Pawn_ApparelTracker
{
    [Feature(nameof(EventDefOf.PostApparelChanged))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
    public static void Notify_ApparelChanged_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostApparelChanged, ___pawn);
    }
}
