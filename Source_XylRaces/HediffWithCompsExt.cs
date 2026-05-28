using Verse;

namespace XylXenos;

public class HediffWithCompsExt : HediffWithComps
{
    public virtual float PartEfficiencyOffset => CurStage.partEfficiencyOffset;

    public override bool TendableNow(bool ignoreTimer = false)
    {
        if (!base.TendableNow(ignoreTimer))
            return false;

        foreach (var comp in comps)
        {
            if (comp is HediffComp_GrowthModeExt { AllowTend: false })
                return false;
        }

        return true;
    }
}
