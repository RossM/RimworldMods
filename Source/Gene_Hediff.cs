using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Gene_Hediff : Gene
    {
        public List<Hediff> LinkedHediffs;

        public override void PostAdd()
        {
            var extension = def.GetModExtension<ModExtension_GeneDef>();
            if (Active && extension?.hediffGivers != null)
            {
                LinkedHediffs = new List<Hediff>();
                foreach (var hediffGiver in extension.hediffGivers) 
                    hediffGiver.TryApply(pawn, LinkedHediffs);
            }
            base.PostAdd();
        }

        public override void PostRemove()
        {
            if (LinkedHediffs != null)
            {
                foreach (var linkedHediff in LinkedHediffs)
                    pawn.health.RemoveHediff(linkedHediff);
            }

            base.PostRemove();
        }
    }
}
