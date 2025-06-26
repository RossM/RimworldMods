using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace XylRacesCore
{
    internal class JobDriver_MilkHuman : JobDriver
    {
        private float gatherProgress;

        private const float WorkTotal = 400f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref gatherProgress, "gatherProgress", 0f);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        void Gather(Pawn doer)
        {
            var gene = Target.FirstGeneOfType<Gene_Hyperlactation>();
            if (gene == null)
                return;

            foreach (var thoughtDef in gene.milkedThoughts)
                Target.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, doer);

            var lactationCharge = gene.LactationCharge;
            if (lactationCharge == null)
                return;
            
            ThingDef milkDef = DefDatabase<ThingDef>.GetNamed("Milk");

            int qty = gene.MilkCount;
            lactationCharge.GreedyConsume(gene.ChargePerItem * qty);

            if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
            {
                MoteMaker.ThrowText((doer.DrawPos + Target.DrawPos) / 2f, Target.Map, "TextMote_ProductWasted".Translate(), 3.65f);
                return;
            }

            while (qty > 0)
            {
                int stackQty = Math.Min(qty, milkDef.stackLimit);
                Log.Message(string.Format("MilkHuman: charge = {0}, qty = {1}, stackQty = {2}", lactationCharge.Charge, qty, stackQty));
                Thing thing = ThingMaker.MakeThing(milkDef);
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
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            var hideClothes = new Comp_RenderProperties { hideClothes = true };
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                actor.pather.StopDead();
                PawnUtility.ForceWait(Target, 15000, null, maintainPosture: true);
                Target?.AllComps.Add(hideClothes);
                Target?.Drawer.renderer.SetAllGraphicsDirty();
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Pawn actor = toil.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f * (float)delta);
                gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * (float)delta;
                if (gatherProgress >= WorkTotal)
                {
                    Gather(actor);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            toil.AddFinishAction(delegate
            {
                if (Target != null && Target.CurJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    Target.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                Target?.AllComps.Remove(hideClothes);
                Target?.Drawer.renderer.SetAllGraphicsDirty();
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
