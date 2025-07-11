using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_HostilityOverride : DefModExtension
    {
        public FactionDef disableHostilityFromFaction;
        public int violationDisableTicks = 400;
    }

    public class HostilityOverride : Gene, INotifyPawnDamagedThing
    {
        private GeneDefExtension_HostilityOverride DefExt => def.GetModExtension<GeneDefExtension_HostilityOverride>();

        public int lastHostileActionTick = int.MinValue;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastHostileActionTick, nameof(lastHostileActionTick), int.MinValue);
        }

        public void Notify_PawnDamagedThing(Thing thing, DamageInfo damageInfo, DamageWorker.DamageResult DamageResult)
        {
            if (DisableHostilityFrom(thing))
            {
                lastHostileActionTick = Find.TickManager.TicksGame;
            }
        }

        private bool DisableHostilityFrom(Thing thing)
        {
            return DefExt.disableHostilityFromFaction != null && DefExt.disableHostilityFromFaction == thing.Faction?.def;
        }

        public bool DisableHostility(Thing thing)
        {
            return Active && Find.TickManager.TicksGame >= lastHostileActionTick + DefExt.violationDisableTicks && DisableHostilityFrom(thing);
        }
    }
}