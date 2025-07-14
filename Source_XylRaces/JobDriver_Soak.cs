using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobDriver_Soak : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();

            Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            toil.tickIntervalAction = _ =>
            {
                if (Find.TickManager.TicksGame > startTick + job.def.joyDuration ||
                    need_wetness is { CurLevel: > 0.9999f })
                {
                    EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    CheckForSwimmingPose();
                }
            };
            yield return toil;
            Toil goToil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            goToil.tickIntervalAction = _ =>
            {
                if (Find.TickManager.TicksGame > startTick + job.def.joyDuration ||
                    need_wetness is { CurLevel: > 0.999f })
                {
                    EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    CheckForSwimmingPose();
                }
            };
            goToil.AddFinishAction(CheckForSwimmingPose);
            yield return goToil;
            Toil toil2 = Toils_General.Wait(240);
            toil2.tickIntervalAction = _ =>
            {
                CheckForSwimmingPose(); 
            };
            yield return toil2;
            Toil toil3 = ToilMaker.MakeToil();
            toil3.initAction = () =>
            {
                if (pawn.health?.hediffSet != null &&
                    pawn.health.hediffSet.TryGetHediff(HediffDefOf.Heatstroke, out var hediff))
                    pawn.health.RemoveHediff(hediff);

                if (!JobGiver_GetWetness.TryFindWaterTile(pawn, out IntVec3 targetTile, 10))
                    return;
                job.targetA = targetTile;
                JumpToToil(goToil);
            };
            yield return toil3;
        }

        private void CheckForSwimmingPose()
        {
            job.swimming = pawn.Position.GetTerrain(pawn.Map).IsWater;
        }

    }
}
