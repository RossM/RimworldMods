using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class DefExt_GeneDependent : DefModExtension
    {
        public List<GeneDef> genePrerequisites;

        public bool Validate()
        {
            if (genePrerequisites == null) 
                return true;

            foreach (var gene in genePrerequisites)
            {
                if (!Faction.OfPlayer.GetPawns().Any(p => p.HasActiveGene(gene)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
