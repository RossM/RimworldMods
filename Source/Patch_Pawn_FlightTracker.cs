using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Jobs;
using Verse;
using Verse.AI;


namespace XylRacesCore
{
    [HarmonyPatch(typeof(Pawn_FlightTracker))]
    public static class Patch_Pawn_FlightTracker
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
        public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = __instance.Get<Pawn>("pawn");
            if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is { } gene)
            {
                gene.Notify_JobStarted(job);
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
