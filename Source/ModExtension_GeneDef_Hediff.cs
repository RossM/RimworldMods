using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_Hediff : DefModExtension
    {
        public List<HediffGiver> hediffGivers;
        public bool applyImmediately = false;
        public float mtbDays = 0.0f;
    }
}
