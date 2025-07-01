using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class WorkGiver_MilkHuman : WorkGiver_Scanner
    {
        private JobDef JobDef => DefDatabase<JobDef>.GetNamed("XylMilkHuman");

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return PotentialWorkThingsGlobal(pawn).All(thing => !HasJobOnThing(pawn, thing, forced));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn target)
                return false;
            if (pawn == target)
                return false;

            var gene = target.FirstGeneOfType<Gene_Hyperlactation>();
            if (gene == null)
                return false;

            if (!gene.allowMilking)
                return false;
            if (!target.CanCasuallyInteractNow())
                return false;
            if (!pawn.CanReserve(target))
                return false;

            return gene.ReadyToMilk();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(JobDef, t);
        }
    }
}
