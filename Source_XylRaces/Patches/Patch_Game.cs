namespace XylXenos.Patches;

[HarmonyPatch(typeof(Game))]
public static class Patch_Game
{
    [Feature(nameof(NotificationDefOf.GlobalPostGameDispose))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.Dispose))]
    public static void Dispose_Postfix()
    {
        NotificationManager.Instance.Notify(NotificationDefOf.GlobalPostGameDispose, null);
        NotificationManager.Instance.Reset();
    }
}
