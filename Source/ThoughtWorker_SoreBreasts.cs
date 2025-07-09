using System;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThoughtWorker_SoreBreasts : ThoughtWorker
    {
        private const int MaxSorenessLevel = 2;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ThoughtUtility.ThoughtNullified(p, def))
                return ThoughtState.Inactive;

            var gene = p.FirstGeneOfType<Gene_Hyperlactation>();
            if (gene == null)
                return ThoughtState.Inactive;
 
            if (!gene.TryGetSoreness(out int soreness))
                return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(Math.Min(soreness, MaxSorenessLevel));
        }
    }
}
