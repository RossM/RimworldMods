using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public class Patch_Pawn_FlightTracker
    {
        private static readonly FieldInfo pawnField = AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        [HarmonyPrefix]
        public static bool Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = (Pawn)pawnField.GetValue(__instance);
            if (pawn.IsPlayerControlled && pawn.genes?.GetFirstGeneOfType<Gene_Flight>() is { } gene)
            {
                gene.Notify_JobStarted(job);
                return true;
            }

            return false;
        }
    }
}
