using Verse;

namespace XylXenos;

public class HediffWithCompsExt : HediffWithComps
{
    public virtual float PartEfficiencyOffset => CurStage.partEfficiencyOffset;
}
