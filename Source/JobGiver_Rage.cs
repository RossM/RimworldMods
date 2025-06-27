using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class JobGiver_Rage : ThinkNode_JobGiver
    {
        private const int MinMeleeChaseTicks = 420;

        private const int MaxMeleeChaseTicks = 900;

        private float maxAttackDistance = 40f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.TryGetAttackVerb(null) == null)
            {
                return null;
            }
            Thing thing = FindAttackTarget(pawn);
            if (thing != null)
            {
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, thing);
                job.maxNumMeleeAttacks = 1;
                job.expiryInterval = Rand.Range(MinMeleeChaseTicks, MaxMeleeChaseTicks);
                job.canBashDoors = true;
                return job;
            }
            return null;
        }

        private Thing FindAttackTarget(Pawn pawn)
        {
            return (Thing)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedReachable, IsGoodTarget, 0f, maxAttackDistance, canBashDoors: true);
        }

        protected virtual bool IsGoodTarget(Thing thing)
        {
            return thing is Pawn { Spawned: not false, Downed: false } pawn && !pawn.IsPsychologicallyInvisible();
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var obj = (JobGiver_Rage)base.DeepCopy(resolve);
            obj.maxAttackDistance = maxAttackDistance;
            return obj;
        }
    }
}
