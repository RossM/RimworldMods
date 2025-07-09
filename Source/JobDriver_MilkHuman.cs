using RimWorld;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobDriver_MilkHuman : JobDriver
    {
        private float gatherProgress;

        private const float WorkTotal = 400f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref gatherProgress, "gatherProgress");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private void Gather(Pawn doer)
        {
            var gene = Target.FirstGeneOfType<Gene_Hyperlactation>();
            if (gene == null)
                return;

            foreach (var thoughtDef in gene.DefExt.milkedThoughts)
                Target.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, doer);

            var lactationCharge = gene.Lactating;
            if (lactationCharge == null)
                return;

            int qty = gene.MilkCount;
            lactationCharge.GreedyConsume(gene.DefExt.chargePerItem * qty);

            if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
            {
                MoteMaker.ThrowText((doer.DrawPos + Target.DrawPos) / 2f, Target.Map, "TextMote_ProductWasted".Translate(), 3.65f);
                return;
            }

            while (qty > 0)
            {
                int stackQty = Math.Min(qty, gene.DefExt.item.stackLimit);
                Thing thing = ThingMaker.MakeThing(gene.DefExt.item);
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
                var comp = Target?.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                    comp.hideClothes = true;
                Target?.rotationTracker.FaceTarget(actor);
            };
            toil.tickIntervalAction = delegate (int delta)
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

                var comp = Target?.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                    comp.hideClothes = false;
            });
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.AddEndCondition(() => Target.FirstGeneOfType<Gene_Hyperlactation>()?.MilkCount is > 0 ? JobCondition.Ongoing : JobCondition.Incompletable);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithProgressBar(TargetIndex.A, () => gatherProgress / WorkTotal);
            toil.activeSkill = () => SkillDefOf.Animals;
            yield return toil;
        }
    }
}
