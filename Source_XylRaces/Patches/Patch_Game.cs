namespace XylXenos.Patches;

[HarmonyPatch(typeof(Game))]
public static class Patch_Game
{
    [Feature(typeof(GeneTracker))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.Dispose))]
    public static void Dispose_Postfix()
    {
        NotificationManager.Instance.Notify(NotificationEvent.GlobalPostGameDispose, null);
        NotificationManager.Instance.Reset();
    }
}
