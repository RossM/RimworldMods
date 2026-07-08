namespace Xylib.Patches;

[HarmonyPatch(typeof(HediffSet))]
internal static class Patch_HediffSet
{
    [Feature(nameof(EventDefOf.PostHediffsChanged))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HediffSet.DirtyCache))]
    public static void DirtyCache_Postfix(HediffSet __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostHediffsChanged, __instance.pawn);
    }
}
