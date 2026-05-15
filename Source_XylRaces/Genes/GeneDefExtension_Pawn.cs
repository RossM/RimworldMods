using System.Collections.Generic;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Pawn : GeneDefExtension
    {
        public float bodySizeFactor = 1.0f;
        public float healthScaleFactor = 1.0f;

        protected override IEnumerable<string> GetCustomEffectDescriptions()
        {
            if (bodySizeFactor != 1.0f)
                yield return $"{"BodySize".Translate()} {(bodySizeFactor - 1f).ToStringPercentSigned()}";
            if (healthScaleFactor != 1.0f)
                yield return $"{"StatsReport_Health".Translate()} {(healthScaleFactor - 1f).ToStringPercentSigned()}";
        }
    }
}
