namespace XylXenos;

public interface INeed
{
    public int CurStage { get; }
}

[UsedFromXml]
public class DefModExtension_Thought_Need : DefModExtension
{
    public NeedDef need;
    public List<float> stages;
}

[UsedFromXml]
public class ThoughtWorker_Need : ThoughtWorker
{
    public DefModExtension_Thought_Need DefExt => def.GetModExtension<DefModExtension_Thought_Need>();

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var need = p.needs.TryGetNeed(DefExt.need);
        if (need == null)
            return ThoughtState.Inactive;

        int stage;

        if (need is INeed iNeed)
            stage = iNeed.CurStage;
        else
        {
            float needLevel = need.CurLevel;

            for (stage = DefExt.stages.Count - 1; stage >= 0; stage--)
            {
                var minLevel = DefExt.stages[stage];
                if (minLevel <= needLevel)
                    break;
            }
        }

        if (stage < 0 || stage >= def.stages.Count || def.stages[stage] == null)
            return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(stage);
    }
}
