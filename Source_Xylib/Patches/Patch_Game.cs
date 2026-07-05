namespace Xylib.Patches;

[HarmonyPatch(typeof(Game))]
internal static class Patch_Game
{
    [Feature(nameof(EventDefOf.GlobalPostGameDispose))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.Dispose))]
    public static void Dispose_Postfix()
    {
        EventManager.Instance.Notify(EventDefOf.GlobalPostGameDispose, null);
        EventManager.Instance.Reset();
    }

    [Feature(typeof(EventManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game.LoadGame))]
    public static void LoadGame_Postfix()
    {
        EventManager.Instance.LoadedGame();
    }
}
