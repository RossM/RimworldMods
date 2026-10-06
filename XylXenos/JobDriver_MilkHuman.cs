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
        return target?.ActiveGeneCompsOfType<GeneComp_Milkable>().Any(comp => comp.ReadyToMilk) is true;
    }

    private void Gather(Pawn doer)
    {
        // ReSharper disable once UseNullPropagation
        if (Target is null)
            return;

        var comp = Target.ActiveGeneCompsOfType<GeneComp_Milkable>().FirstOrDefault(comp => comp.ReadyToMilk);
        if (comp == null)
            return;

        DebugAssert.NotNull(Target.Map);
        DebugAssert.NotNull(doer.Map);

        comp.Notify_Milked(doer);

        if (comp.Chargeable is not { } chargeable)
            return;

        int qty = comp.ItemCount;
        chargeable.GreedyConsume(comp.Props.chargePerItem * qty);

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
        Pawn? actor = toil.actor;

        DebugAssert.NotNull(actor);
        DebugAssert.NotNull(actor.skills);

        actor.skills.Learn(SkillDefOf.Animals, 0.13f * delta);
        gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * delta;
        if (gatherProgress >= WorkTotal)
        {
            Gather(actor);
            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
    }
}
