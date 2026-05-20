using System;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class ThoughtDefExtension_PawnStatus : DefModExtension
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
        public ThoughtDefExtension_PawnStatus DefExt => def.GetModExtension<ThoughtDefExtension_PawnStatus>();

        private bool Check(Pawn p)
        {
            return DefExt.status switch
            {
                ThoughtDefExtension_PawnStatus.StatusMode.Any => true,
                ThoughtDefExtension_PawnStatus.StatusMode.Slave => p.IsSlave,
                ThoughtDefExtension_PawnStatus.StatusMode.NotSlave => !p.IsSlave,
                ThoughtDefExtension_PawnStatus.StatusMode.Prisoner => p.IsPrisoner,
                ThoughtDefExtension_PawnStatus.StatusMode.NotPrisoner => !p.IsPrisoner,
                ThoughtDefExtension_PawnStatus.StatusMode.Freeman => p.IsFreeman,
                ThoughtDefExtension_PawnStatus.StatusMode.NotFreeman => !p.IsFreeman,
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
}
