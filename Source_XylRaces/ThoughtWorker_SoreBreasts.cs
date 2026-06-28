namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_SoreBreasts : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var comp = p.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>();
        if (comp == null)
            return ThoughtState.Inactive;

        var sorenessStage = comp.SorenessStage;
        return sorenessStage >= 0
            ? ThoughtState.ActiveAtStage(Math.Min(sorenessStage, def.stages.Count - 1))
            : ThoughtState.Inactive;
    }
}
