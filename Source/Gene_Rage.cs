using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_Rage : DefModExtension
    {
        public float chance = 1.0f;
        public HediffDef hediffDef;
        public float severity;
    }

    public class Gene_Rage : Gene
    {
        public ModExtension_GeneDef_Rage DefExt => def.GetModExtension<ModExtension_GeneDef_Rage>();

        public void Notify_DamageTaken(DamageInfo dinfo, DamageWorker.DamageResult damageResult)
        {
            Log.Message(string.Format("Gene_Berserker.Notify_DamageTaken({0}, {1})", dinfo, damageResult));

            if (!Rand.Chance(DefExt.chance))
                return;
            if (pawn.Downed)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef) ?? pawn.health.AddHediff(DefExt.hediffDef);
            if (hediff != null)
                hediff.ageTicks = 0;
        }
    }
}
