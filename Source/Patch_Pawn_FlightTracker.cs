using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Pawn_FlightTracker))]
    public class Patch_Pawn_FlightTracker
    {
        private static readonly FieldInfo pawnField = AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        [HarmonyPrefix, HarmonyPatch(nameof(Pawn_FlightTracker.Notify_JobStarted))]
        public static bool Notify_JobStarted_Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = (Pawn)pawnField.GetValue(__instance);
            if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is { } gene)
            {
                gene.Notify_JobStarted(job);
                return false;
            }

            return true;
        }

        public static void FlightTick_Prefix(Pawn_FlightTracker __instance)
        {
            var pawn = (Pawn)pawnField.GetValue(__instance);
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
