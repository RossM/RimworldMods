using XylXenos;

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
        if (target.Downed)
            return false;

        var gene = target.FirstActiveGeneOfType<Gene_Hyperlactation>();
        return gene is { allowMilking: true } && gene.ReadyToMilk();
    }

    private void Gather(Pawn doer)
    {
        var gene = Target.FirstActiveGeneOfType<Gene_Hyperlactation>();
        if (gene == null)
            return;

        if (!gene.HyperlactationInfo.milkedThoughts.NullOrEmpty())
        {
            foreach (var thoughtDef in gene.HyperlactationInfo.milkedThoughts)
                Target.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, doer);
        }

        var lactationCharge = gene.Lactating;
        if (lactationCharge == null)
            return;

        int qty = gene.MilkCount;
        lactationCharge.GreedyConsume(gene.HyperlactationInfo.chargePerItem * qty);

        if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
        {
            MoteMaker.ThrowText((doer.DrawPos + Target.DrawPos) / 2f, Target.Map, "TextMote_ProductWasted".Translate(), 3.65f);
            return;
        }

        while (qty > 0)
        {
            int stackQty = Math.Min(qty, gene.HyperlactationInfo.item.stackLimit);
            Thing thing = ThingMaker.MakeThing(gene.HyperlactationInfo.item);
            thing.stackCount = stackQty;
            qty -= stackQty;
            if (!GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near))
                return;
        }
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDowned(TargetIndex.A);
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
        toil.AddEndCondition(() =>
            Target.FirstActiveGeneOfType<Gene_Hyperlactation>()?.MilkCount is > 0 ? JobCondition.Ongoing : JobCondition.Incompletable);
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.WithProgressBar(TargetIndex.A, () => gatherProgress / WorkTotal);
        toil.activeSkill = () => SkillDefOf.Animals;
        yield return toil;
    }
}