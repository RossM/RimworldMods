using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneDefExtension_SeeingRed : GeneDefExtension
    {
        public float chance = 1.0f;
        public HediffDef hediffDef;
    }

    public class SeeingRed : Gene, INotificationTarget
    {
        public GeneDefExtension_SeeingRed DefExt => def.GetModExtension<GeneDefExtension_SeeingRed>();

        const int checkInterval = 60;
        public HashSet<Thing> extraEnemies;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref extraEnemies, nameof(extraEnemies), LookMode.Reference);
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(checkInterval, delta))
                return;
            if (extraEnemies != null)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);
                if (hediff == null)
                    extraEnemies.Clear();
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

        public void Notify_DamageTaken(DamageInfo damageInfo)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediffDef);

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

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register<DamageInfo>(NotificationCategory.DamageTaken, pawn, Notify_DamageTaken);
        }
    }
}
