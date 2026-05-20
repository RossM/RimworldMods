using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(WorkGiver_Warden_Feed))]
    public static class Patch_WorkGiver_Warden_Feed
    {
        [Feature(typeof(Hediff_DietDependency))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WorkGiver_Warden_Feed.JobOnThing))]
        public static void JobOnThing_Postfix(WorkGiver_Warden_Feed __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (__result != null)
                return;

            if (!__instance.ShouldTakeCareOfPrisoner(pawn, t, forced))
            {
                return;
            }

            Pawn prisoner = (Pawn)t;

            if (!WardenFeedUtility.ShouldBeFed(prisoner))
            {
                return;
            }

            foreach (var hediff in prisoner.HediffsOfType<Hediff_DietDependency>().Where(h => h.ShouldSatisfy)
                         .OrderByDescending(h => h.Severity))
            {
                //Log.Message($"JobOnThing_Postfix: pawn: {pawn}, prisoner: {prisoner}, hediff: {hediff}, severity: {hediff.Severity}");
                Thing foodSource = hediff.FindFoodFor(pawn);
                if (foodSource == null)
                    continue;
                ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                Job job = JobMaker.MakeJob(JobDefOf.FeedPatient, foodSource, prisoner);
                job.count = hediff.Gene.ItemsWantedToSatisfy(foodSource, foodDef);
                __result = job;
                return;
            }
        }
    }
}
