using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(WorkGiver_Warden_Feed))]
    public static class Patch_WorkGiver_Warden_Feed
    {
        public static Func<WorkGiver_Warden_Feed, Pawn, Thing, bool, bool> ShouldTakeCareOfPrisoner =
            AccessTools.MethodDelegate<Func<WorkGiver_Warden_Feed, Pawn, Thing, bool, bool>>(
                AccessTools.Method(typeof(WorkGiver_Warden_Feed), "ShouldTakeCareOfPrisoner"));

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(WorkGiver_Warden_Feed.JobOnThing))]
        public static void JobOnThing_Postfix(WorkGiver_Warden_Feed __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (__result != null)
                return;

            if (!ShouldTakeCareOfPrisoner(__instance, pawn, t, forced))
            {
                return;
            }

            Pawn pawn2 = (Pawn)t;

            if (!WardenFeedUtility.ShouldBeFed(pawn2))
            {
                return;
            }

            foreach (var hediff in pawn2.HediffsOfType<Hediff_DietDependency>().Where(h => h.ShouldSatisfy).OrderByDescending(h => h.Severity))
            {
                //Log.Message(string.Format("JobOnThing_Postfix: pawn: {0}, pawn2: {1}, hediff: {2}, severity: {3}", pawn, pawn2, hediff, hediff.Severity));
                Thing foodSource = hediff.FindFoodFor(pawn2);
                if (foodSource == null)
                    continue;
                ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                Job job = JobMaker.MakeJob(JobDefOf.FeedPatient, foodSource, pawn2);
                job.count = hediff.Gene.ItemsWantedToSatisfy(foodSource, foodDef);
                __result = job;
                return;
            }
        }
    }
}
