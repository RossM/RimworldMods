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
            {
                return ThoughtState.Inactive;
            }
            if (ThoughtUtility.ThoughtNullified(p, def))
            {
                return ThoughtState.Inactive;
            }

            int colonistCount = p.Map.mapPawns.ColonistsSpawnedCount;
            if (colonistCount <= Thought_Situational_HerdInstinct.NumPawns_Alone)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            if (colonistCount <= Thought_Situational_HerdInstinct.NumPawns_SmallHerd)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (colonistCount <= Thought_Situational_HerdInstinct.NumPawns_Inactive)
            {
                return ThoughtState.Inactive;
            }
            if (colonistCount <= Thought_Situational_HerdInstinct.NumPawns_LargeHerd)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            return ThoughtState.ActiveAtStage(3);
        }
    }
}
