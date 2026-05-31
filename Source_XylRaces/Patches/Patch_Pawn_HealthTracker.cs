namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_HealthTracker))]
public static class Patch_Pawn_HealthTracker
{
    [Feature(nameof(NotificationDefOf.PostCheckForStateChange))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_HealthTracker.CheckForStateChange))]
    public static void CheckForStateChange_Postfix(Pawn_HealthTracker __instance)
    {
        NotificationManager.Instance.Notify(NotificationDefOf.PostCheckForStateChange, __instance.pawn);
    }
}
