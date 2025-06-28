using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class MentalState_SeeingRed : MentalState
    {
        public override bool ForceHostileTo(Thing t)
        {
            return pawn.HasGeneOfType<Gene_SeeingRed>(g => g.ForceHostility(t));
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}
