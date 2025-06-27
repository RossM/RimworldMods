using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class GeneDefExtension_HostilityOverride : DefModExtension
    {
        public FactionDef disableHostilityFromFaction;
        public AnimalType? disableHostilityFromAnimalType;
        public int violationDisableTicks = 400;
    }

    public class Gene_HostilityOverride : Gene
    {
        private GeneDefExtension_HostilityOverride DefExt => def.GetModExtension<GeneDefExtension_HostilityOverride>();

        public int lastHostileActionTick = int.MinValue;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastHostileActionTick, "lastHostileActionTick", int.MinValue);
        }

        public void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult DamageResult)
        {
            if (DisableHostilityFrom(thing))
            {
                lastHostileActionTick = Find.TickManager.TicksGame;
            }
        }

        private bool DisableHostilityFrom(Thing thing)
        {
            if (DefExt.disableHostilityFromFaction != null && DefExt.disableHostilityFromFaction == thing.Faction?.def)
                return true;
            if (DefExt.disableHostilityFromAnimalType != null && DefExt.disableHostilityFromAnimalType == (thing as Pawn)?.RaceProps.animalType)
                return true;

            return false;
        }

        public bool DisableHostility(Thing thing)
        {
            return Active && Find.TickManager.TicksGame >= lastHostileActionTick + DefExt.violationDisableTicks && DisableHostilityFrom(thing);
        }
    }
}