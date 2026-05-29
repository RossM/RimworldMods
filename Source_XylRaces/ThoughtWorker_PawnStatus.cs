namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Thought_PawnStatus : DefModExtension
{
    public enum StatusMode
    {
        Any,
        Slave,
        NotSlave,
        Prisoner,
        NotPrisoner,
        Freeman,
        NotFreeman,
    }

    public StatusMode status = StatusMode.Any;
}

[UsedFromXml]
public class ThoughtWorker_PawnStatus : ThoughtWorker
{
    public DefModExtension_Thought_PawnStatus DefExt => def.GetModExtension<DefModExtension_Thought_PawnStatus>();

    private bool Check(Pawn p)
    {
        return DefExt.status switch
        {
            DefModExtension_Thought_PawnStatus.StatusMode.Any => true,
            DefModExtension_Thought_PawnStatus.StatusMode.Slave => p.IsSlave,
            DefModExtension_Thought_PawnStatus.StatusMode.NotSlave => !p.IsSlave,
            DefModExtension_Thought_PawnStatus.StatusMode.Prisoner => p.IsPrisoner,
            DefModExtension_Thought_PawnStatus.StatusMode.NotPrisoner => !p.IsPrisoner,
            DefModExtension_Thought_PawnStatus.StatusMode.Freeman => p.IsFreeman,
            DefModExtension_Thought_PawnStatus.StatusMode.NotFreeman => !p.IsFreeman,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        return Check(p) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
    }
}