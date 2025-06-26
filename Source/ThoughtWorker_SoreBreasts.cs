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
    public class ThoughtWorker_SoreBreasts : ThoughtWorker
    {
        private const int TicksPerStage = 60000;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
            {
                //Log.Message("ThoughtWorker_SoreBreasts: Not spawned");
                return ThoughtState.Inactive;
            }
            if (ThoughtUtility.ThoughtNullified(p, def))
            {
                //Log.Message("ThoughtWorker_SoreBreasts: Nullified");
                return ThoughtState.Inactive;
            }

            var gene = p.FirstGeneOfType<Gene_Hyperlactation>();
            if (gene == null)
            {
                //Log.Message("ThoughtWorker_SoreBreasts: No gene");
                return ThoughtState.Inactive;
            }

            if (gene.fullSinceTick == null)
            {
                //Log.Message("ThoughtWorker_SoreBreasts: Not full");
                return ThoughtState.Inactive;
            }

            //Log.Message(string.Format("ThoughtWorker_SoreBreasts: {0} - {1}", Find.TickManager.TicksGame, gene.fullSinceTick.Value));
            return ThoughtState.ActiveAtStage(Math.Min(Mathf.FloorToInt((float)(Find.TickManager.TicksGame - gene.fullSinceTick.Value) / TicksPerStage), 2));
        }
    }
}
