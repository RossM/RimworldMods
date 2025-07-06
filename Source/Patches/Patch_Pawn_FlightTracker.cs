using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_FlightTracker))]
    public static class Patch_Pawn_FlightTracker
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
        public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = __instance.Get<Pawn>("pawn");
            if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is not null)
            {
                return false;
            }

            return true;
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_FlightTracker.FlightTick))]
        public static void FlightTick_Prefix(Pawn_FlightTracker __instance)
        {
            var pawn = __instance.Get<Pawn>("pawn");
            if (pawn.Downed && !pawn.Position.WalkableBy(pawn.Map, pawn))
            {
                if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is { } gene)
                {
                    gene.Notify_Downed();
                }
            }
        }
    }
}
