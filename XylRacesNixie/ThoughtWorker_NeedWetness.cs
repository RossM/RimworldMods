using RimWorld;
using Verse;

namespace XylRacesNixie
{
    public class ThoughtWorker_NeedWetness : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.needs.TryGetNeed<Need_Wetness>()?.CurCategory == WetnessCategory.Parched)
                return ThoughtState.ActiveAtStage(0);
            if (p.needs.TryGetNeed<Need_Wetness>()?.CurCategory == WetnessCategory.VeryDry)
                return ThoughtState.ActiveAtStage(1);
            if (p.needs.TryGetNeed<Need_Wetness>()?.CurCategory == WetnessCategory.Dry)
                return ThoughtState.ActiveAtStage(2);
            if (p.needs.TryGetNeed<Need_Wetness>()?.CurCategory == WetnessCategory.Wet)
                return ThoughtState.ActiveAtStage(3);
            return ThoughtState.Inactive;
        }
    }
}
