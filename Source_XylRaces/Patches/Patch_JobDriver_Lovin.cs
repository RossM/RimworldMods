namespace XylXenos.Patches;

[HarmonyPatch(typeof(JobDriver_Lovin))]
public static class Patch_JobDriver_Lovin
{
    [Feature(typeof(NotificationManager))]
    [HarmonyPostfix]
    [HarmonyPatch("<MakeNewToils>b__12_4")]
    public static void MakeNewToils_Postfix(JobDriver_Lovin __instance)
    {
        Pawn partner = (Pawn)(Thing)__instance.job.GetTarget(TargetIndex.A);
        NotificationManager.Instance.Notify(NotificationDefOf.PostLovin, __instance.pawn, partner);
    }
}
