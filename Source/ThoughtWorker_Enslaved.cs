using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThoughtWorker_Enslaved : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ThoughtUtility.ThoughtNullified(p, def))
                return ThoughtState.Inactive;

            return p.IsSlave ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
}
