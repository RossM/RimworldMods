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
        public List<Gene> addedGenes;

        public override void PostAdd()
        {
            base.PostAdd();

            if (Rand.Chance(0.5f))
            {
                AddGene(DefDatabase<GeneDef>.AllDefsListForReading.RandomElementByWeight(GeneWeight));

                if (Rand.Chance(0.5f))
                {
                    AddGene(DefDatabase<GeneDef>.GetNamed("Instability_Mild"));
                }
            }
        }

        private void AddGene(GeneDef geneDef)
        {
            addedGenes ??= new List<Gene>();
            if (geneDef != null && !pawn.genes.HasActiveGene(geneDef))
                addedGenes.Add(pawn.genes.AddGene(geneDef, IsXenogene));
        }

        private bool IsXenogene => pawn.genes.Xenogenes.Contains(this);

        float GeneWeight(GeneDef def)
        {
            // No genes requiring archite capsules
            if (def.biostatArc != 0)
                return 0.0f;
            // No purely cosmetic genes
            if (def.biostatCpx == 0)
                return 0.0f;
            // No genes with too large an effect on metabolic efficiency
            if (def.biostatMet is < -2 or > 2)
                return 0.0f;

            // No genes with requirements, unless they are met by the pawn's xenotype
            if (def.prerequisite != null && !pawn.genes.Xenotype.AllGenes.Contains(def.prerequisite))
                return 0.0f;

            // No genes that conflict with genes in the pawn's xenotype
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

            if (addedGenes == null) 
                return;

            foreach (var gene in addedGenes)
                pawn.genes.RemoveGene(gene);
        }
    }
}
