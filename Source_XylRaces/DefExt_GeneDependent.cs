using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class DefExt_GeneDependent : DefModExtension
    {
        public List<GeneDef> genePrerequisitesAny;
        public List<MemeDef> memePrerequisitesAny;

        public bool Validate()
        {
            if (genePrerequisitesAny.NullOrEmpty() && memePrerequisitesAny.NullOrEmpty())
                return true;

            if (!memePrerequisitesAny.NullOrEmpty())
            {
                foreach (MemeDef item in memePrerequisitesAny)
                {
                    if (Faction.OfPlayer.ideos.HasAnyIdeoWithMeme(item))
                        return true;
                }
            }

            if (!genePrerequisitesAny.NullOrEmpty())
            {
                foreach (var gene in genePrerequisitesAny)
                {
                    if (Faction.OfPlayer.GetPawns().Any(p => p.HasActiveGene(gene)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
