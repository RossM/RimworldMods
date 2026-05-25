using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneIngestionThoughtsOverride
    {
        public ThingDef thing;
        public List<MeatSourceCategory> meatSources;
        public List<ThoughtDef> thoughts;
    }

    public class GeneDefExtension_IngestionThoughtOverride : GeneDefExtension
    {
        public List<GeneIngestionThoughtsOverride> thoughtOverrides;
    }
}
