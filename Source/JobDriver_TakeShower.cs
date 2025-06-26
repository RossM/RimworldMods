using System.Collections.Generic;
using Verse;
using Verse.AI;

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

            var startTick = Find.TickManager.TicksGame;

            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.defaultCompleteMode = ToilCompleteMode.Delay;
            work.defaultDuration = job.def.joyDuration;
            work.WithProgressBar(TargetIndex.A, () => (float)(Find.TickManager.TicksGame - startTick) / job.def.joyDuration);
            work.AddPreTickIntervalAction((int delta) =>
            {
                if (need_wetness != null && need_wetness.CurLevel > 0.9999f)
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            });
            var comp = new Comp_RenderProperties { hideClothes = true };
            work.initAction = () =>
            {
                if (need_wetness != null)
                    need_wetness.IsShowering = true;
                pawn.AllComps.Add(comp);
            };
            work.AddFinishAction(() =>
            {
                if (need_wetness != null)
                    need_wetness.IsShowering = false;
                pawn.AllComps.Remove(comp);
            });
            yield return work;
        }
    }
}
