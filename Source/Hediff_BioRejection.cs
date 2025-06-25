using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Hediff_BioRejection : Hediff
    {
        public override float Severity
        {
            get => pawn.health.hediffSet.CountAddedAndImplantedParts() * 1.0f;
            set { }
        }
    }
}
