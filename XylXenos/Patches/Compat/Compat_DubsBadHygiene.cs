namespace XylXenos.Patches.Compat;

[Patch]
[Category(ModIdOf.DubsBadHygiene)]
public static class Compat_DubsBadHygiene
{
    [Feature(FeatureNameOf.Compat)]
    [Postfix]
    [Inner(typeof(Toil), nameof(Toil.AddFinishAction))]
    [Target("DubsBadHygiene.JobDriver_takeBath.MakeNewToils")]
    [Target("DubsBadHygiene.JobDriver_takeShower.MakeNewToils")]
    [Target("DubsBadHygiene.JobDriver_useWashBucket.MakeNewToils")]
    [Target("DubsBadHygiene.JobDriver_washPatient.MakeNewToils")]
    public static void AddFinishAction_Postfix(Toil __instance, [Field("job", Scope.Outer)] Job job, [Field("pawn", Scope.Outer)] Pawn pawn)
    {
        Pawn washedPawn = job.def.defName == "washPatient" ? (Pawn)job.targetA.Thing : pawn;
        DebugAssert.False(washedPawn.Dead);

        if (__instance.endConditions is not { Count: >= 1 })
            return;
        if (washedPawn.needs.TryGetNeed<Need_Wetness>() is not { } need)
            return;
        if (job.def.GetModExtension<DefModExtension_Job_WetnessSource>() is not { } wetnessSourceJob)
            return;
        DefModExtension_Thing_WetnessSource? wetnessSourceThing
            = job.targetA.Thing?.def.GetModExtension<DefModExtension_Thing_WetnessSource>();

        if (!wetnessSourceJob.neverEndsEarly)
        {
            var oldEndCondition = __instance.endConditions[^1];

            __instance.endConditions[^1] = () =>
            {
                var result = oldEndCondition();
                if (result == JobCondition.Ongoing)
                    return result;
                if (need.CurLevel < (wetnessSourceJob.wetnessLevel ?? wetnessSourceThing?.wetnessLevel ?? 1.0f))
                    return JobCondition.Ongoing;
                return JobCondition.Succeeded;
            };
        }

        if (wetnessSourceJob.overrideDuration is { } duration)
            __instance.defaultDuration = duration;
    }
}
