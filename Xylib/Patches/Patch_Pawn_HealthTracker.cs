namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_HealthTracker))]
public static class Patch_Pawn_HealthTracker
{
    [Feature(nameof(EventDefOf.PostCheckForStateChange))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_HealthTracker.CheckForStateChange))]
    public static void CheckForStateChange_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostCheckForStateChange, ___pawn);
    }

    [Feature(nameof(EventDefOf.PostDowned))]
    [HarmonyPostfix]
    [HarmonyPatch("MakeDowned")]
    public static void MakeDowned_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostDowned, ___pawn);
    }
}
