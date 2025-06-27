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

        public bool DisableHostilityFrom(Thing thing)
        {
            var pawn = thing as Pawn;
            if (disableHostilityFromFaction != null && disableHostilityFromFaction == (thing.Faction?.def ?? pawn?.kindDef?.defaultFactionDef)) 
                return true;
            if (disableHostilityFromAnimalType != null && disableHostilityFromAnimalType == pawn?.RaceProps.animalType)
                return true;

            return false;
        }
    }
}
