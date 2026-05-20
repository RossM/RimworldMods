using Verse;

namespace XylXenos
{
    public class Hediff_Petrification : HediffWithComps
    {
        public PetrificationGrowthMode GrowthMode =>
            GetComp<HediffComp_GrowthMode_Petrification>()?.growthMode ?? PetrificationGrowthMode.Active;

        public override float PainOffset => GrowthMode == PetrificationGrowthMode.Active ? base.PainOffset : 0f;
    }
}
