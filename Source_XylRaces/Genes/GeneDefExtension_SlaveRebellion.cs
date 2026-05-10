using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_SlaveRebellion : GeneDefExtension
    {
        // I am using a gene def extension rather than a StatDef here because I don't want to have
        // to account for every way the stat might be modified in the rebellion interval tooltip
        public float slaveRebellionMtbFactor = 1.0f;
        public float neverRebelThresholdDays = -1f;
    }
}
