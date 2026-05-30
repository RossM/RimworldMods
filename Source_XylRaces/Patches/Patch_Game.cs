namespace XylXenos.Patches;

[HarmonyPatch(typeof(Game))]
public static class Patch_Game
{
    [Feature(typeof(GeneSet))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.Dispose))]
    public static void Dispose_Postfix()
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostGameDispose, null);
        NotificationManager.Instance.Reset();
    }
}