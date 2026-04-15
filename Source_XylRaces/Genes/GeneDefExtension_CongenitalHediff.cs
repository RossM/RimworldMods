using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_CongenitalHediff : DefModExtension
    {
        public List<HediffGiver> hediffGivers;
        public float chance = 1.0f;
    }
}
