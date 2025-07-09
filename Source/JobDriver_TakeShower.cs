using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class JobDriver_TakeShower : JobDriver
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly]
            public static EffecterDef XylShowerSplash;
        }

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

            Toil toil = ToilMaker.MakeToil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = job.def.joyDuration;
            //toil.WithProgressBar(TargetIndex.A, () => (float)(Find.TickManager.TicksGame - startTick) / job.def.joyDuration);
            toil.AddPreTickIntervalAction(_ =>
            {
                if (need_wetness is { CurLevel: > 0.9999f })
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            });
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                if (need_wetness != null)
                    need_wetness.IsShowering = true;
                var comp = actor.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                    comp.hideClothes = comp.hideHeadgear = true;
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
                var comp = actor.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                    comp.hideClothes = comp.hideHeadgear = true;
            });
            EffecterDef effecterDef = Defs.XylShowerSplash;
            toil.WithEffect(effecterDef, TargetIndex.A);
            toil.handlingFacing = true;
            yield return toil;
        }
    }
}
