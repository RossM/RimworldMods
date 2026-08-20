namespace XylXenos;

[UsedFromXml]
public class JobDriver_CastWithHeldThing : JobDriver_CastAbility
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() =>
        {
            DebugAssert.NotNull(job.ability);
            return !job.ability.CanCast && !job.ability.Casting;
        });
        Ability? ability = (job.verbToUse as Verb_CastAbility)?.ability;
        DebugAssert.NotNull(ability);
        yield return Toils_General.DoAtomic(() => { job.count = 1; });
        yield return Toils_General.DoAtomic(delegate
        {
            DebugAssert.NotNull(pawn);
            if (pawn.IsCarrying())
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        });
        yield return Toils_Reserve.Reserve(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOn(() => !ability.CanApplyOn(job.targetA));
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Combat.CastVerb(TargetIndex.B, canHitNonTargetPawns: false);
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        job.ability?.Notify_StartedCasting();
    }
}
