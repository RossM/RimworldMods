using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Gene_Atavism : Gene
    {
        public Gene addedGene;

        public override void PostAdd()
        {
            base.PostAdd();

            if (Rand.Chance(0.5f))
            {
                var geneDef = DefDatabase<GeneDef>.AllDefsListForReading.RandomElementByWeight(GeneWeight);
                addedGene = pawn.genes.AddGene(geneDef, pawn.genes.Xenogenes.Contains(this));
            }
        }

        float GeneWeight(GeneDef def)
        {
            if (def.biostatArc != 0)
                return 0.0f;
            if (def.biostatCpx == 0)
                return 0.0f;
            if (def.biostatMet is < -3 or > 3)
                return 0.0f;
            foreach (var gene in pawn.genes.Xenotype.AllGenes)
            {
                if (def == gene)
                    return 0.0f;
                if (def.exclusionTags != null && gene.exclusionTags != null && def.exclusionTags.Union(gene.exclusionTags).Any())
                    return 0.0f;
            }
            return def.selectionWeight;
        }

        public override void PostRemove()
        {
            base.PostRemove();

            if (addedGene != null)
                pawn.genes.RemoveGene(addedGene);
        }
    }
}
