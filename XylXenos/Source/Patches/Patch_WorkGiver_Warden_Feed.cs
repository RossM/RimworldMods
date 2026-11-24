namespace XylXenos.Patches;

[Patch(typeof(WorkGiver_Warden_Feed))]
public static class Patch_WorkGiver_Warden_Feed
{
    [Feature(typeof(Hediff_DietDependency))]
    [Postfix]
    [Target(nameof(WorkGiver_Warden_Feed.JobOnThing))]
    public static void JobOnThing_Postfix(
        WorkGiver_Warden_Feed __instance,
        Pawn pawn,
        Thing t,
        bool forced,
        ref Job? __result,
        [Method] Func<Pawn, Thing, bool, bool> ShouldTakeCareOfPrisoner)
    {
        if (__result is not null)
            return;

        if (!ShouldTakeCareOfPrisoner(pawn, t, forced))
            return;

        Pawn prisoner = (Pawn)t;

        if (!WardenFeedUtility.ShouldBeFed(prisoner))
            return;

        foreach (var hediff in prisoner.HediffsOfType<Hediff_DietDependency>().Where(h => h.ShouldSatisfy)
                     .OrderByDescending(h => h.Severity))
        {
            //Log.Message($"JobOnThing_Postfix: pawn: {pawn}, prisoner: {prisoner}, hediff: {hediff}, severity: {hediff.Severity}");
            if (hediff.FindFoodFor(pawn) is not { } foodSource)
                continue;
            if (FoodUtility.GetFinalIngestibleDef(foodSource) is not { } foodDef)
                continue;

            Job job = JobMaker.MakeJob(JobDefOf.FeedPatient, foodSource, prisoner);
            job.count = hediff.ItemsWantedToSatisfy(foodSource, foodDef);
            __result = job;
            return;
        }
    }
}
