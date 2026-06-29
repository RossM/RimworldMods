namespace XylXenos;

[UsedFromXml]
public class JobDriver_MilkHuman : JobDriver_InteractWithPawn
{
    private const float WorkTotal = 400f;
    private float gatherProgress;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gatherProgress, nameof(gatherProgress));
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
    }

    public override bool ValidateTarget(Pawn target)
    {
        return target?.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>() is { ReadyToMilk: true };
    }

    private void Gather(Pawn doer)
    {
        var comp = Target.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>();
        if (comp == null)
            return;

        comp.Notify_Milked(doer);

        var lactationCharge = comp.Lactating;
        if (lactationCharge == null)
            return;

        int qty = comp.MilkCount;
        lactationCharge.GreedyConsume(comp.Props.chargePerItem * qty);


        if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
        {
            MoteMaker.ThrowText((doer.DrawPos + Target.DrawPos) / 2f, Target.Map, "TextMote_ProductWasted".Translate(), 3.65f);
            return;
        }

        while (qty > 0)
        {
            int stackQty = Math.Min(qty, comp.Props.item.stackLimit);
            Thing thing = ThingMaker.MakeThing(comp.Props.item);
            thing.stackCount = stackQty;
            qty -= stackQty;
            if (!GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near))
                return;
        }
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
            Pawn actor = toil.actor;
            actor.pather.StopDead();
            PawnUtility.ForceWait(Target, 15000, maintainPosture: true);

            Target?.rotationTracker.FaceTarget(actor);
        };
        toil.tickIntervalAction = delegate(int delta)
        {
            Pawn actor = toil.actor;
            actor.skills.Learn(SkillDefOf.Animals, 0.13f * delta);
            gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * delta;
            if (gatherProgress >= WorkTotal)
            {
                Gather(actor);
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        };
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
        toil.WithProgressBar(TargetIndex.A, () => gatherProgress / WorkTotal);
        toil.activeSkill = () => SkillDefOf.Animals;
        yield return toil;
    }
}
