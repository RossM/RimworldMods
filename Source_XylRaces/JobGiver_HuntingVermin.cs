namespace XylXenos;

[UsedFromXml]
public class JobGiver_HuntingVermin : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        Job job;

        if (pawn.MentalState is not MentalState_HuntingVermin mentalState)
            return null;

        if (mentalState.target?.Corpse is Corpse corpse)
        {
            if (!pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Some))
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);

            job = JobMaker.MakeJob(JobDefOf.Ingest, corpse);
            return job;
        }

        if (!mentalState.IsTargetStillValidAndReachable())
            return null;

        DebugAssert.NotNull(mentalState.target);

        Thing? targetThing = mentalState.target.SpawnedParentOrMe;
        job = JobMaker.MakeJob(JobDefOf.AttackMelee, targetThing);
        job.killIncappedTarget = true;
        if (targetThing != mentalState.target)
            job.maxNumMeleeAttacks = 2;
        return job;
    }
}
