using XylXenos;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_FlightTracker))]
public static class Patch_Pawn_FlightTracker
{
    [Feature(typeof(Gene_Flight))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
    {
        var pawn = __instance?.pawn;
        if (pawn is { IsPlayerControlled: true } && pawn.HasActiveGeneOfType<Gene_Flight>())
        {
            return false;
        }

        return true;
    }

    // Note: This patch is performance-sensitive
    [Feature(typeof(Gene_Flight))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_FlightTracker.FlightTick))]
    public static void FlightTick_Prefix(Pawn_FlightTracker __instance)
    {
        var pawn = __instance.pawn;
        if (__instance.Flying && pawn.Downed && !pawn.Position.WalkableBy(pawn.Map, pawn))
        {
            if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is { } gene)
            {
                gene.Notify_Downed();
            }
        }
    }
}