using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Gene_Psycast : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.psychicEntropy.SetInitialPsyfocusLevel();
        }
    }
}
