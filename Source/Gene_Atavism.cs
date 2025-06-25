using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_Atavism : DefModExtension
    {
        public int biostatArcMin, biostatArcMax;
        public int biostatCpxMin, biostatCpxMax;
        public int biostatMetMin, biostatMetMax;
        public float geneChance = 1.0f;
        public float extraGeneChance = 1.0f;
        public List<GeneDef> extraGenes;
    }

    public class Gene_Atavism : Gene
    {
        public List<Gene> addedGenes;
        public ModExtension_GeneDef_Atavism DefExt => def.GetModExtension<ModExtension_GeneDef_Atavism>();

        public override void PostAdd()
        {
            base.PostAdd();

            if (Rand.Chance(DefExt.geneChance))
            {
                AddGene(DefDatabase<GeneDef>.AllDefsListForReading.RandomElementByWeight(GeneWeight));

                if (DefExt.extraGenes != null && Rand.Chance(DefExt.extraGeneChance))
                {
                    AddGene(DefExt.extraGenes.RandomElement());
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
            if (def.biostatArc < DefExt.biostatArcMin || def.biostatArc > DefExt.biostatArcMax)
                return 0.0f;
            if (def.biostatCpx < DefExt.biostatCpxMin || def.biostatCpx > DefExt.biostatCpxMax)
                return 0.0f;
            if (def.biostatMet < DefExt.biostatMetMin || def.biostatMet > DefExt.biostatMetMax)
                return 0.0f;

            // No genes with requirements, unless they are met by the pawn's xenotype
            if (def.prerequisite != null && !pawn.genes.Xenotype.AllGenes.Contains(def.prerequisite))
                return 0.0f;

            // No genes that conflict with genes in the pawn's xenotype
            foreach (var gene in pawn.genes.Xenotype.AllGenes)
            {
                if (def == gene)
                    return 0.0f;
                if (def.exclusionTags != null && gene.exclusionTags != null &&
                    def.exclusionTags.Union(gene.exclusionTags).Any())
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