using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneExt : Gene
    {
        public GeneDefExt DefExt => (GeneDefExt)def;

        public override bool Active
        {
            get
            {
                if (!base.Active)
                    return false;
                if (DefExt.gender != null && DefExt.gender != pawn.gender)
                    return false;
                if (DefExt.geneType == GeneType.Endogene && !pawn.genes.HasEndogene(def))
                    return false;
                if (DefExt.geneType == GeneType.Xenogene && !pawn.genes.HasXenogene(def))
                    return false;
                return true;
            }
        }
    }
}
