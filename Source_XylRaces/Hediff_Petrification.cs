using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class Hediff_Petrification : HediffWithComps
    {
        public PetrificationGrowthMode GrowthMode =>
            GetComp<HediffComp_PetrificationGrowthMode>()?.growthMode ?? PetrificationGrowthMode.Active;

        public override float PainOffset => GrowthMode == PetrificationGrowthMode.Active ? base.PainOffset : 0f;
    }
}
