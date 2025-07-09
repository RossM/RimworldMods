using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class JobDriver_TakeShower : JobDriver
    {
        public bool showering;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref showering, "showering");
        }

        [DefOf]
        private static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
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

            Toil toil = ToilMaker.MakeToil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = job.def.joyDuration;
            toil.AddPreTickIntervalAction(_ =>
            {
                if (need_wetness is { CurLevel: > 0.9999f })
                    EndJobWith(JobCondition.Succeeded);

            });
            toil.initAction = () =>
            {
                showering = true;
                var comp = toil.actor.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                {
                    comp.job = job;
                    comp.hideClothes = comp.hideHeadgear = true;
                }

                if (pawn.health?.hediffSet != null && pawn.health.hediffSet.TryGetHediff(HediffDefOf.Heatstroke, out var hediff))
                    pawn.health.RemoveHediff(hediff);
            };
            toil.tickIntervalAction = delta =>
            {
                // Occasionally change facing randomly
                Pawn actor = toil.actor;
                if (actor.IsHashIntervalTick(200, delta) && Rand.Chance(0.5f))
                    actor.Rotation = Rot4.Random;
            };
            EffecterDef effecterDef = Defs.XylShowerSplash;
            toil.WithEffect(effecterDef, TargetIndex.A);
            toil.handlingFacing = true;
            yield return toil;
        }
    }
}
