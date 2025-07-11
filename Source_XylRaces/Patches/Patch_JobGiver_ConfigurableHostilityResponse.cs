using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse))]
    public static class Patch_JobGiver_ConfigurableHostilityResponse
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("TryGiveJob")]
        public static bool TryGiveJob_Prefix(Pawn pawn, ref Job __result)
        {
            using (new ProfileBlock())
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
}
