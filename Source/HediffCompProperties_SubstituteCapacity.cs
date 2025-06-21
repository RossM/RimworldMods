using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class HediffCompProperties_SubstituteCapacity : HediffCompProperties
    {
        public enum SubstitutionMode
        {
            Always,
            Maximum,
            Minimum,
        }

        public SubstitutionMode mode;
        public PawnCapacityDef originalCapacity;
        public PawnCapacityDef substituteCapacity;

        public HediffCompProperties_SubstituteCapacity()
        {
            compClass = typeof(HediffComp_SubstituteCapacity);
        }
    }
}
