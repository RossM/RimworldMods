using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob")]
    public class Patch_JobGiver_ConfigurableHostilityResponse
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn.health.hediffSet.hediffs.OfType<Hediff_ForceBehavior>().Any())
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}
