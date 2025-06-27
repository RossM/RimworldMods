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
        private GeneDefExtension_HostilityOverride DefExt =>
            def.GetModExtension<GeneDefExtension_HostilityOverride>();

        public int lastHostileActionTick = -99999;

        public void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult DamageResult)
        {
            if (DefExt.DisableHostilityFrom(thing))
            {
                lastHostileActionTick = Find.TickManager.TicksGame;
            }
        }

        public bool DisableHostility(Thing thing)
        {
            return Active && Find.TickManager.TicksGame >= lastHostileActionTick + DefExt.violationDisableTicks && DefExt.DisableHostilityFrom(pawn);
        }
    }
}
