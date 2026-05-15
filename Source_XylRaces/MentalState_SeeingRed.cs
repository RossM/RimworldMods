using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class MentalState_SeeingRed : MentalState
    {
        public override bool ForceHostileTo(Thing t)
        {
            return pawn.HasActiveGeneOfType<SeeingRed>(g => g.ForceHostility(t));
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}
