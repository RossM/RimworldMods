namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_ApparelTracker))]
public static class Patch_Pawn_ApparelTracker
{
    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
    public static void Notify_ApparelChanged_Postfix(Pawn_ApparelTracker __instance)
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostApparelChanged, __instance.pawn);
    }
}
