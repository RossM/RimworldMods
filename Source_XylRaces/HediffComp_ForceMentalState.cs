using JetBrains.Annotations;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class HediffCompProperties_ForceMentalState : HediffCompProperties
    {
        public MentalStateDef mentalState;
        public bool endMentalStateOnCure = true;

        public HediffCompProperties_ForceMentalState()
        {
            compClass = typeof(HediffComp_ForceMentalState);
        }
    }

    public class HediffComp_ForceMentalState : HediffComp
    {
        public HediffCompProperties_ForceMentalState Props => (HediffCompProperties_ForceMentalState)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            Pawn.mindState.mentalStateHandler.TryStartMentalState(Props.mentalState, forced: true, forceWake: true, causedByDamage: true);
        }

        public override void CompPostPostRemoved()
        {
            if (Props.endMentalStateOnCure && Pawn.mindState.mentalStateHandler.CurStateDef == Props.mentalState &&
                !Pawn.mindState.mentalStateHandler.CurState.causedByMood)
            {
                Pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
            }
        }
    }
}
