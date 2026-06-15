namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_FlightTracker))]
public static class Patch_Pawn_FlightTracker
{
    [Feature(typeof(Gene_Flight))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static bool Notify_JobStarted_Prefix(Pawn ___pawn, Job job)
    {
        return !___pawn.HasActiveGeneOfType<Gene_Flight>();
    }
}
