using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobDriver_CastAbilityStandingOn : JobDriver_CastAbility
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);
            Ability ability = ((Verb_CastAbility)job.verbToUse).ability;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOn(() => !ability.CanApplyOn(job.targetA));
            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }
    }

}
