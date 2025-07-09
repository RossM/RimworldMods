using RimWorld;
using System.Collections.Generic;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneIngestionThoughtsOverride
    {
        public ThingDef thing;
        public List<MeatSourceCategory> meatSources;
        public List<ThoughtDef> thoughts;
    }

    public class GeneDefExtension_IngestionThoughtOverride : DefModExtension
    {
        public List<GeneIngestionThoughtsOverride> thoughtOverrides;
    }
}
