namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_SoreBreasts : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var sorenessStage = p.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>().SorenessStage;
        return sorenessStage >= 0
            ? ThoughtState.ActiveAtStage(Math.Min(sorenessStage, def.stages.Count - 1))
            : ThoughtState.Inactive;
    }
}
