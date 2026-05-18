using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace XylXenos.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_CongenitalHediff : GeneDefExtension
    {
        public List<HediffGiver> hediffGivers;
        public float chance = 1.0f;
    }
}
