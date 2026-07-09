namespace XylXenos;

[UsedFromXml]
public class JobGiver_SeeingRed : ThinkNode_JobGiver
{
    private const int MinMeleeChaseTicks = 420;

    private const int MaxMeleeChaseTicks = 900;

    private float maxAttackDistance = 40f;

    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (pawn.TryGetAttackVerb(null) == null)
        {
            return null;
        }

        if (FindAttackTarget(pawn) is not { } thing)
            return null;

        Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, thing);
        job.expiryInterval = Rand.Range(MinMeleeChaseTicks, MaxMeleeChaseTicks);
        job.canBashDoors = true;
        return job;
    }

    private Thing? FindAttackTarget(Pawn pawn)
    {
        return (Thing?)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedReachable, IsGoodTarget, 0f, maxAttackDistance,
            canBashDoors: true);
    }

    protected virtual bool IsGoodTarget(Thing thing)
    {
        return thing is Pawn { Spawned: true, Downed: false } pawn && !pawn.IsPsychologicallyInvisible() ||
               thing is Building { Spawned: true, def.building.IsTurret: true };
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var obj = (JobGiver_SeeingRed)base.DeepCopy(resolve);
        obj.maxAttackDistance = maxAttackDistance;
        return obj;
    }
}
