using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Gene_HostilityOverride : Gene
    {
        private ModExtension_GeneDef_HostilityOverride DefExtension =>
            def.GetModExtension<ModExtension_GeneDef_HostilityOverride>();

        public int lastHostileActionTick = -99999;

        public void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult DamageResult)
        {
            if (DefExtension.DisableHostilityFrom(thing))
            {
                lastHostileActionTick = Find.TickManager.TicksGame;
            }
        }

        public bool DisableHostilityFrom(Pawn pawn)
        {
            return Active && Find.TickManager.TicksGame >= lastHostileActionTick + DefExtension.violationDisableTicks && DefExtension.DisableHostilityFrom(pawn);
        }
    }
}
