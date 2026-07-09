namespace XylXenos;

public abstract class JobDriver_InteractWithPawn : JobDriver
{
    // ReSharper disable once MemberCanBeProtected.Global
    public Pawn? Target => TargetPawnA;
    protected new abstract SkillDef ActiveSkill { get; }
    protected abstract bool HasProgressBar { get; }
    protected abstract float Progress { get; }

    public abstract bool ValidateTarget(Pawn? target);

    protected abstract void InteractionTickInterval(Toil toil, int delta);

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnNotCasualInterruptible(TargetIndex.A);
        this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil toil = ToilMaker.MakeToil();
        toil.initAction = delegate
        {
            DebugAssert.NotNull(toil.actor);
            DebugAssert.NotNull(toil.actor.pather);
            DebugAssert.NotNull(Target);
            DebugAssert.NotNull(Target.rotationTracker);

            Pawn actor = toil.actor;
            actor.pather.StopDead();
            PawnUtility.ForceWait(Target, 15000, maintainPosture: true);

            Target.rotationTracker.FaceTarget(actor);
        };
        toil.tickIntervalAction = delta => { InteractionTickInterval(toil, delta); };
        toil.AddFinishAction(() =>
        {
            if (Target != null && Target.CurJobDef == JobDefOf.Wait_MaintainPosture)
            {
                Target.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        });
        toil.FailOnDespawnedOrNull(TargetIndex.A);
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        toil.AddEndCondition(() => ValidateTarget(Target) ? JobCondition.Ongoing : JobCondition.Incompletable);
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        if (HasProgressBar)
            toil.WithProgressBar(TargetIndex.A, () => Progress);
        toil.activeSkill = () => ActiveSkill;
        yield return toil;
    }
}
