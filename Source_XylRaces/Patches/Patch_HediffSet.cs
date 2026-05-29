namespace XylXenos.Patches;

[HarmonyPatch(typeof(HediffSet))]
public class Patch_HediffSet
{
    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HediffSet.DirtyCache))]
    public static void DirtyCache_Postfix(HediffSet __instance)
    {
        NotificationManager.Instance.Notify(NotificationEvent.PostHediffsChanged, __instance.pawn);
    }
}