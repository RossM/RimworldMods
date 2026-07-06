namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_JobTracker))]
internal static class Patch_Pawn_JobTracker
{
    [Feature(nameof(EventDefOf.PostJobStarted))]
    [InfixPostfix(typeof(JobDriver), nameof(JobDriver.ReadyForNextToil))]
    [InfixPatch(nameof(Pawn_JobTracker.StartJob))]
    public static void ReadyForNextToil_Postfix(Pawn ___pawn, JobDriver __instance)
    {
        EventManager.Instance.Notify(EventDefOf.PostJobStarted, ___pawn, __instance);
    }
}
