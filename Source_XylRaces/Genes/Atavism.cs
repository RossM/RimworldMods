using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Atavism : DefModExtension
    {
        public IntRange biostatArc;
        public IntRange biostatCpx;
        public IntRange biostatMet;
        public float geneChance = 1.0f;
        public float extraGeneChance = 1.0f;
        public List<GeneDef> extraGenes;
    }

    [UsedImplicitly]
    public class Atavism : Gene
    {
        public List<Gene> addedGenes;
        public GeneDefExtension_Atavism DefExt => def.GetModExtension<GeneDefExtension_Atavism>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
        }

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
            if (geneDef != null && !pawn.genes.HasActiveGene(geneDef))
                (addedGenes ??= []).Add(pawn.genes.AddGene(geneDef, IsXenogene));
        }

        private bool IsXenogene => pawn.genes.Xenogenes.Contains(this);

        private float GeneWeight(GeneDef geneDef)
        {
            if (geneDef.biostatArc < DefExt.biostatArc.min || geneDef.biostatArc > DefExt.biostatArc.max)
                return 0.0f;
            if (geneDef.biostatCpx < DefExt.biostatCpx.min || geneDef.biostatCpx > DefExt.biostatCpx.max)
                return 0.0f;
            if (geneDef.biostatMet < DefExt.biostatMet.min || geneDef.biostatMet > DefExt.biostatMet.max)
                return 0.0f;

            // No genes with requirements, unless they are met by the pawn's xenotype
            if (geneDef.prerequisite != null && !pawn.genes.Xenotype.AllGenes.Contains(geneDef.prerequisite))
                return 0.0f;

            // No genes that conflict with genes in the pawn's xenotype
            foreach (var gene in pawn.genes.Xenotype.AllGenes)
            {
                if (geneDef == gene)
                    return 0.0f;
                if (geneDef.exclusionTags != null && gene.exclusionTags != null &&
                    geneDef.exclusionTags.Union(gene.exclusionTags).Any())
                    return 0.0f;
            }

            return geneDef.selectionWeight;
        }

        public override void PostRemove()
        {
            base.PostRemove();

            if (addedGenes == null)
                return;

            foreach (var gene in addedGenes)
                pawn.genes.RemoveGene(gene);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismChanceLabel".TranslateSimple(),
                DefExt.geneChance.ToStringPercent(), "XylAtavismChanceDesc".TranslateSimple(), 1002);
            if (addedGenes == null) 
                yield break;
            string text = string.Join(", ", addedGenes.Select(g => g.Label)).CapitalizeFirst();
            yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismGenesLabel".TranslateSimple(), 
                text, "XylAtavismGenesDesc".TranslateSimple(), 1001);
        }
    }
}