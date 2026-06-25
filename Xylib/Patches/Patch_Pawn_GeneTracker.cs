namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_GeneTracker))]
public static class Patch_Pawn_GeneTracker
{
    [Feature(nameof(EventDefOf.PostGenesChanged))]
    [HarmonyPostfix]
    [HarmonyPatch("Notify_GenesChanged")]
    public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostGenesChanged, __instance.pawn);
    }
}
