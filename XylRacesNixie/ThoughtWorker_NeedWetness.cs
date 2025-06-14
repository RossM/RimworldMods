using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesNixie
{
    public class ThoughtWorker_NeedWetness : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p.needs.TryGetNeed<Need_Wetness>()?.CurCategory switch
            {
                WetnessCategory.Parched => ThoughtState.ActiveAtStage(0),
                WetnessCategory.VeryDry => ThoughtState.ActiveAtStage(1),
                WetnessCategory.Dry => ThoughtState.ActiveAtStage(2),
                WetnessCategory.Wet => ThoughtState.ActiveAtStage(3),
                _ => ThoughtState.Inactive,
            };
        }
    }
}
