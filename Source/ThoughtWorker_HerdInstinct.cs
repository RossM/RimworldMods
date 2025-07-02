using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThoughtWorker_HerdInstinct : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
                return ThoughtState.Inactive;
            if (ThoughtUtility.ThoughtNullified(p, def))
                return ThoughtState.Inactive;

            int colonistCount = p.Map.mapPawns.ColonistsSpawnedCount;
            return colonistCount switch
            {
                <= Thought_Situational_HerdInstinct.NumPawns_Alone => ThoughtState.ActiveAtStage(0),
                <= Thought_Situational_HerdInstinct.NumPawns_SmallHerd => ThoughtState.ActiveAtStage(1),
                <= Thought_Situational_HerdInstinct.NumPawns_Inactive => ThoughtState.Inactive,
                <= Thought_Situational_HerdInstinct.NumPawns_LargeHerd => ThoughtState.ActiveAtStage(2),
                _ => ThoughtState.ActiveAtStage(3)
            };
        }
    }
}
