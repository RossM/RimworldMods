namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_ApparelTracker))]
public static class Patch_Pawn_ApparelTracker
{
    [Feature(nameof(EventDefOf.PostApparelChanged))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
    public static void Notify_ApparelChanged_Postfix(Pawn_ApparelTracker __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostApparelChanged, __instance.pawn);
    }
}
