namespace XylXenos;

[UsedFromXml]
public class JobDriver_MilkHuman : JobDriver_InteractWithPawn
{
    protected override SkillDef ActiveSkill => SkillDefOf.Animals;

    protected override bool HasProgressBar => true;

    protected override float Progress => gatherProgress / WorkTotal;
    private const float WorkTotal = 400f;
    private float gatherProgress;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gatherProgress, nameof(gatherProgress));
    }

    public override bool ValidateTarget(Pawn? target)
    {
        return target?.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>() is { ReadyToMilk: true };
    }

    private void Gather(Pawn doer)
    {
        var comp = Target?.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>();
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

    protected override void InteractionTickInterval(Toil toil, int delta)
    {
        Pawn actor = toil.actor;
        actor.skills.Learn(SkillDefOf.Animals, 0.13f * delta);
        gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * delta;
        if (gatherProgress >= WorkTotal)
        {
            Gather(actor);
            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
    }
}
