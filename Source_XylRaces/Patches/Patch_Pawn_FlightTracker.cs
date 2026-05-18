using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_FlightTracker))]
    public static class Patch_Pawn_FlightTracker
    {
        [Feature(typeof(Flight))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
        public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = __instance?.pawn;
            if (pawn is { IsPlayerControlled: true } && pawn.HasActiveGeneOfType<Flight>())
            {
                return false;
            }

            return true;
        }

        // Note: This patch is performance-sensitive
        [Feature(typeof(Flight))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn_FlightTracker.FlightTick))]
        public static void FlightTick_Prefix(Pawn_FlightTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn.Downed && !pawn.Position.WalkableBy(pawn.Map, pawn))
            {
                if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Flight>() is { } gene)
                {
                    gene.Notify_Downed();
                }
            }
        }
    }
}
