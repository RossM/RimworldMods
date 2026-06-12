namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_FlightTracker))]
public static class Patch_Pawn_FlightTracker
{
    [Feature(typeof(Gene_Flight))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
    {
        return !__instance.pawn.HasActiveGeneOfType<Gene_Flight>();
    }
}
