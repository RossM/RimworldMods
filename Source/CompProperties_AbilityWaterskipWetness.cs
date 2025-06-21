using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace XylRacesCore
{
    public class CompProperties_AbilityWaterskipWetness : CompProperties_AbilityEffect
    {
        public float satisfaction = 0.5f;

        public CompProperties_AbilityWaterskipWetness()
        {
            compClass = typeof(CompAbilityEffect_WaterskipWetness);
        }
    }
}
