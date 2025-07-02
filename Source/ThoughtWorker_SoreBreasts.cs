using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    // ReSharper disable once UnusedMember.Global
    public class ThoughtWorker_SoreBreasts : ThoughtWorker
    {
        private const int MaxSorenessLevel = 2;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
                return ThoughtState.Inactive;
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
