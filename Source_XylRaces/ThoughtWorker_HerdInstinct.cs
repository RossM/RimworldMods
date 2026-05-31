namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_HerdInstinct : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.Spawned || ThoughtUtility.ThoughtNullified(p, def) || p.Faction != Faction.OfPlayer)
            return ThoughtState.Inactive;

        return p.Map.mapPawns.ColonistsSpawnedCount switch
        {
            <= Thought_Situational_HerdInstinct.NumPawns_Alone => ThoughtState.ActiveAtStage(0),
            <= Thought_Situational_HerdInstinct.NumPawns_SmallHerd => ThoughtState.ActiveAtStage(1),
            <= Thought_Situational_HerdInstinct.NumPawns_Inactive => ThoughtState.Inactive,
            <= Thought_Situational_HerdInstinct.NumPawns_LargeHerd => ThoughtState.ActiveAtStage(2),
            _ => ThoughtState.ActiveAtStage(3)
        };
    }
}
