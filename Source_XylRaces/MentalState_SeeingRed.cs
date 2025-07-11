using RimWorld;
using JetBrains.Annotations;
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
            return pawn.HasGeneOfType<SeeingRed>(g => g.ForceHostility(t));
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}
