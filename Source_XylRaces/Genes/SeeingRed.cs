using RimWorld;
using System.Collections.Generic;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_SeeingRed : DefModExtension
    {
        public float chance = 1.0f;
        public HediffDef hediffDef;
    }

    public class SeeingRed : Gene, INotifyDamageTaken
    {
        public HashSet<Thing> extraEnemies;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref extraEnemies, nameof(extraEnemies), LookMode.Reference);
        }

        public GeneDefExtension_SeeingRed DefExt => def.GetModExtension<GeneDefExtension_SeeingRed>();

        public void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            Verse.Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);

            if (hediff == null && !Rand.Chance(DefExt.chance))
                return;
            if (pawn.Downed)
                return;

            hediff ??= pawn.health.AddHediff(DefExt.hediffDef);
            if (hediff == null) 
                return;

            (extraEnemies ??= []).Add(damageInfo.Instigator);

            var comp = hediff.TryGetComp<HediffComp_Disappears>();
            if (comp == null) 
                return;
            comp.ticksToDisappear = comp.disappearsAfterTicks;
        }

        public override void TickInterval(int delta)
        {
            using (new ProfileBlock())
            {
                base.TickInterval(delta);
                if (!pawn.IsHashIntervalTick(60, delta)) 
                    return;
                if (extraEnemies != null)
                {
                    Verse.Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);
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
