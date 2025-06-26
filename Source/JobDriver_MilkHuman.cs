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

            var lactationCharge = gene.LactationCharge;
            if (lactationCharge == null)
                return;
            
            ThingDef milkDef = DefDatabase<ThingDef>.GetNamed("Milk");

            int qty = Mathf.FloorToInt(lactationCharge.Charge / gene.ChargePerItem);
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
            Toil wait = ToilMaker.MakeToil("MakeNewToils");
            wait.initAction = delegate
            {
                Pawn actor = wait.actor;
                Pawn obj = (Pawn)job.GetTarget(TargetIndex.A).Thing;
                actor.pather.StopDead();
                PawnUtility.ForceWait(obj, 15000, null, maintainPosture: true);
            };
            wait.tickIntervalAction = delegate (int delta)
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f * (float)delta);
                gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * (float)delta;
                if (gatherProgress >= WorkTotal)
                {
                    Gather(actor);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            wait.AddFinishAction(delegate
            {
                if (Target != null && Target.CurJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    Target.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            });
            wait.FailOnDespawnedOrNull(TargetIndex.A);
            wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            wait.AddEndCondition(() => MilkNutrition(Target) >= 0.25 ? JobCondition.Ongoing : JobCondition.Incompletable);
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.WithProgressBar(TargetIndex.A, () => gatherProgress / WorkTotal);
            wait.activeSkill = () => SkillDefOf.Animals;
            yield return wait;
        }

        private float MilkNutrition(Pawn pawn)
        {
            return pawn.FirstGeneOfType<Gene_Hyperlactation>()?.LactationCharge?.Charge ?? 0;
        }
    }
}
