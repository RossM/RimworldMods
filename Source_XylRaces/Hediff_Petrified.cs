using JetBrains.Annotations;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class Hediff_Petrified : HediffWithComps
    {
        public virtual float PartEfficiencyOffset => CurStage.partEfficiencyOffset * Severity;
    }
}
