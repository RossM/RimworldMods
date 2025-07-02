using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    // ReSharper disable once UnusedMember.Global
    public class ThoughtWorker_Enslaved : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p.IsSlave ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
}
