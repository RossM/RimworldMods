namespace Xylib.Patches;

[HarmonyPatch(typeof(JobDriver_Lovin))]
internal static class Patch_JobDriver_Lovin
{
    [Feature(nameof(EventDefOf.PostLovin))]
    [Postfix]
    [Target("<MakeNewToils>b__12_4")]
    public static void MakeNewToils_Postfix(JobDriver_Lovin __instance)
    {
        Pawn? partner = __instance.job.GetTarget(TargetIndex.A).Pawn;
        EventManager.Instance.Notify(EventDefOf.PostLovin, __instance.pawn, partner);
    }
}
