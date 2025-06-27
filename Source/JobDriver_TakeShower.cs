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
                if (need_wetness != null && need_wetness.CurLevel > 0.9999f)
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            });
            var comp = new Comp_RenderProperties { hideClothes = true };
            toil.initAction = () =>
            {
                if (need_wetness != null)
                    need_wetness.IsShowering = true;
                pawn.AllComps.Add(comp);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                pawn.Rotation = Rot4.South;
            };
            toil.AddFinishAction(() =>
            {
                if (need_wetness != null)
                    need_wetness.IsShowering = false;
                pawn.AllComps.Remove(comp);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            });
            EffecterDef effecterDef = DefDatabase<EffecterDef>.GetNamed("ShowerSplash");
            toil.WithEffect(effecterDef, TargetIndex.A);
            toil.handlingFacing = true;
            yield return toil;
        }
    }
}
