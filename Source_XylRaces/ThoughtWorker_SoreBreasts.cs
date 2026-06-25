namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_SoreBreasts : ThoughtWorker
{
    private const int MaxSorenessLevel = 2;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var comp = p.FirstActiveGeneCompOfType<GeneComp_Hyperlactation>();
        if (comp == null)
            return ThoughtState.Inactive;

        if (!comp.TryGetSoreness(out int soreness))
            return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(Math.Min(soreness, MaxSorenessLevel));
    }
}
