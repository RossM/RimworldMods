using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class GeneDefExtension_SeeingRed : DefModExtension
    {
        public float chance = 1.0f;
        public HediffDef hediffDef;
    }

    public class Gene_SeeingRed : Gene
    {
        public HashSet<Thing> extraEnemies;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref extraEnemies, "extraEnemies", LookMode.Reference);
        }

        public GeneDefExtension_SeeingRed DefExt => def.GetModExtension<GeneDefExtension_SeeingRed>();

        public void Notify_DamageTaken(DamageInfo dinfo, DamageWorker.DamageResult damageResult)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);

            if (hediff == null && !Rand.Chance(DefExt.chance))
                return;
            if (pawn.Downed)
                return;

            hediff ??= pawn.health.AddHediff(DefExt.hediffDef);
            if (hediff == null) 
                return;

            extraEnemies ??= new HashSet<Thing>();
            extraEnemies.Add(dinfo.Instigator);

            var comp = hediff.TryGetComp<HediffComp_Disappears>();
            if (comp == null) 
                return;
            comp.ticksToDisappear = comp.disappearsAfterTicks;
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(60, delta))
            {
                if (extraEnemies != null)
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);
                    if (hediff == null)
                        extraEnemies.Clear();
                }
            }
        }

        public bool ForceHostility(Thing thing)
        {
            return extraEnemies != null && extraEnemies.Contains(thing);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "XylRageChanceLabel".TranslateSimple(),
                DefExt.chance.ToStringPercent(), "XylRageChanceDesc".TranslateSimple(), 1);
        }
    }
}
