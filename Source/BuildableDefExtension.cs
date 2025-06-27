using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class BuildableDefExtension : DefModExtension
    {
        public List<GeneDef> genePrerequisites;

        public bool ValidateBuildable(Map map)
        {
            if (genePrerequisites == null) 
                return true;

            foreach (var gene in genePrerequisites)
            {
                if (!map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                        .Any(p => p.genes?.HasActiveGene(gene) ?? false))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
