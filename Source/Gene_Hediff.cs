using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_Hediff : DefModExtension
    {
        public List<HediffGiver> hediffGivers;
        public bool applyImmediately = false;
        public float mtbDays = 0.0f;
    }

    public class Gene_Hediff : Gene
    {
        public ModExtension_GeneDef_Hediff DefExt => def.GetModExtension<ModExtension_GeneDef_Hediff>();

        public override void PostAdd()
        {
            var extension = DefExt;
            if (Active && extension?.hediffGivers != null && extension.applyImmediately)
            {
                foreach (var hediffGiver in extension.hediffGivers)
                    hediffGiver.TryApply(pawn);
            }

            base.PostAdd();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            var extension = DefExt;
            if (Active && extension?.hediffGivers != null && extension.mtbDays > 0.0f &&
                pawn.IsHashIntervalTick(60, delta))
            {
                foreach (var hediffGiver in extension.hediffGivers)
                {
                    if (Rand.MTBEventOccurs(extension.mtbDays, 60000f, 60f))
                        hediffGiver.TryApply(pawn);
                }
            }
        }

        public override void PostRemove()
        {
            var extension = DefExt;
            if (Active && extension?.hediffGivers != null)
            {
                foreach (var hediffGiver in extension.hediffGivers)
                foreach (var hediff in pawn.health.hediffSet.hediffs.Where(hediff => hediff.def == hediffGiver.hediff)
                             .ToList())
                    pawn.health.RemoveHediff(hediff);
            }

            base.PostRemove();
        }
    }
}