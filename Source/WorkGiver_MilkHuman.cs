using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class WorkGiver_MilkHuman : WorkGiver_Scanner
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
            public static JobDef XylMilkHuman;
        }

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
            return JobMaker.MakeJob(Defs.XylMilkHuman, t);
        }
    }
}
