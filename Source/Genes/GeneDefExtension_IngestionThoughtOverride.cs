using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
