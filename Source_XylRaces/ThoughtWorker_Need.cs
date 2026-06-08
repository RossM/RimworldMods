namespace XylXenos;

[UsedFromXml]
public class ThoughtDefExtension_Need : DefModExtension
{
    public NeedDef need;
    public List<float> stages;
}

[UsedFromXml]
public class ThoughtWorker_Need : ThoughtWorker
{
    public ThoughtDefExtension_Need DefExt => def.GetModExtension<ThoughtDefExtension_Need>();

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var need = p.needs.TryGetNeed(DefExt.need);
        if (need == null)
            return ThoughtState.Inactive;

        float needLevel = need.CurLevel;

        for (int i = DefExt.stages.Count - 1; i >= 0; i--)
        {
            var minLevel = DefExt.stages[i];
            if (minLevel > needLevel)
                continue;
            if (def.stages[i] == null)
                return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(i);
        }

        return ThoughtState.Inactive;
    }
}
