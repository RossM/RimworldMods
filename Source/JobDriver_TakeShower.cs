using System.Collections.Generic;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace XylRacesCore
{
    public class JobDriver_TakeShower : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();

            //var startTick = Find.TickManager.TicksGame;

            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = job.def.joyDuration;
            //toil.WithProgressBar(TargetIndex.A, () => (float)(Find.TickManager.TicksGame - startTick) / job.def.joyDuration);
            toil.AddPreTickIntervalAction((int delta) =>
            {
                if (need_wetness is { CurLevel: > 0.9999f })
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            });
            var comp = new Comp_RenderProperties { hideClothes = true, hideHeadgear = true };
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                if (need_wetness != null)
                    need_wetness.IsShowering = true;
                actor.AllComps.Add(comp);
            };
            toil.tickIntervalAction = delegate(int delta)
            {
                // Occasionally change facing randomly
                Pawn actor = toil.actor;
                if (actor.IsHashIntervalTick(200, delta) && Rand.Chance(0.5f))
                    actor.Rotation = Rot4.Random;
            };
            toil.AddFinishAction(() =>
            {
                Pawn actor = toil.actor;
                if (need_wetness != null)
                    need_wetness.IsShowering = false;
                actor.AllComps.Remove(comp);
            });
            EffecterDef effecterDef = DefDatabase<EffecterDef>.GetNamed("XylShowerSplash");
            toil.WithEffect(effecterDef, TargetIndex.A);
            toil.handlingFacing = true;
            yield return toil;
        }
    }
}
