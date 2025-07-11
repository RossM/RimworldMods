using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobGiver_HuntingVermin : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Job job;

            if (pawn.MentalState is not MentalState_HuntingVermin mentalState_huntingVermin)
                return null;

            if (mentalState_huntingVermin.target.Corpse is { } corpse)
            {
                if (!pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Some))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }

                job = JobMaker.MakeJob(JobDefOf.Ingest, corpse);
                return job;
            }

            if (!mentalState_huntingVermin.IsTargetStillValidAndReachable())
                return null;

            Thing targetThing = mentalState_huntingVermin.target.SpawnedParentOrMe;
            job = JobMaker.MakeJob(JobDefOf.AttackMelee, targetThing);
            job.killIncappedTarget = true;
            if (targetThing != mentalState_huntingVermin.target) 
                job.maxNumMeleeAttacks = 2;
            return job;
        }
    }
}
