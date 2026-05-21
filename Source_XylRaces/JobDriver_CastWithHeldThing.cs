using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylXenos
{
    [UsedFromXml]
    public class JobDriver_CastWithHeldThing : JobDriver_CastAbility
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);
            Ability ability = ((Verb_CastAbility)job.verbToUse).ability;
            yield return Toils_General.DoAtomic(() => { job.count = 1; });
            yield return Toils_General.DoAtomic(delegate
            {
                if (pawn.IsCarrying())
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                }
            });
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOn(() => !ability.CanApplyOn(job.targetA));
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            Toil castVerb = Toils_Combat.CastVerb(TargetIndex.B, canHitNonTargetPawns: false);
            yield return castVerb;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }
    }
}
