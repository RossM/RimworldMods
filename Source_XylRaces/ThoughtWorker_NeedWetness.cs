using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThoughtWorker_NeedWetness : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ThoughtUtility.ThoughtNullified(p, def))
                return ThoughtState.Inactive;

            WetnessCategory? wetnessCategory = p.needs.TryGetNeed<Need_Wetness>()?.CurCategory;
            return wetnessCategory switch
            {
                WetnessCategory.Parched => ThoughtState.ActiveAtStage(0),
                WetnessCategory.VeryDry => ThoughtState.ActiveAtStage(1),
                WetnessCategory.Dry => ThoughtState.ActiveAtStage(2),
                WetnessCategory.Neutral => ThoughtState.Inactive,
                WetnessCategory.Wet => ThoughtState.ActiveAtStage(3),
                _ => ThoughtState.Inactive
            };
        }
    }
}
