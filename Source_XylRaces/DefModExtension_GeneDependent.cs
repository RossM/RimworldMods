using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class DefModExtension_GeneDependent : DefModExtension
    {
        public List<GeneDef> genePrerequisitesAny;

        public bool Validate()
        {
            if (genePrerequisitesAny.NullOrEmpty())
                return true;

            foreach (var gene in genePrerequisitesAny)
            {
                if (Faction.OfPlayer.GetPawns().Any(p => p.HasActiveGene(gene)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
