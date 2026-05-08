using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_BonusGene : DefModExtension
    {
        public IntRange biostatArc = IntRange.Zero;
        public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
        public IntRange biostatMet = new(int.MinValue, int.MaxValue);
        public float geneChance = 1.0f;
        public List<GeneDef> extraGenes;
        public bool removeAfterAdding = false;
        public IntRange count = IntRange.One;
    }

    [UsedImplicitly]
    public class BonusGene : Gene
    {
        public List<Gene> addedGenes;
        public GeneDefExtension_BonusGene DefExt => def.GetModExtension<GeneDefExtension_BonusGene>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
        }

        public override void PostAdd()
        {
            base.PostAdd();

            if (!Rand.Chance(DefExt.geneChance)) 
                return;

            int count = DefExt.count.RandomInRange;

            for (int i = 0; i < count; i++)
            {
                List<GeneDef> genes = !DefExt.extraGenes.NullOrEmpty()
                    ? DefExt.extraGenes
                    : DefDatabase<GeneDef>.AllDefsListForReading;
                AddGene(genes.RandomElementByWeight(GeneWeight));
            }

            if (DefExt.removeAfterAdding)
                pawn.genes.RemoveGene(this);
        }

        private void AddGene(GeneDef geneDef)
        {
            if (geneDef == null)
                return;
            if (pawn.genes.GenesListForReading.Any(g => g.def == geneDef))
                return;

            if (!GeneTuning.BiostatRange.Includes(geneDef.biostatMet +
                                                  pawn.genes.GenesListForReading.Sum(g => g.Active ? g.def.biostatMet : 0)))
                return;

            (addedGenes ??= []).Add(pawn.genes.AddGene(geneDef, IsXenogene));
        }

        private bool IsXenogene => pawn.genes.Xenogenes.Contains(this);

        private float GeneWeight(GeneDef geneDef)
        {
            if (!DefExt.biostatArc.Includes(geneDef.biostatArc))
                return 0.0f;
            if (!DefExt.biostatCpx.Includes(geneDef.biostatCpx))
                return 0.0f;
            if (!DefExt.biostatMet.Includes(geneDef.biostatMet))
                return 0.0f;

            // No genes with requirements, unless they are met by the pawn's xenotype or already added genes
            if (geneDef.prerequisite != null && !pawn.genes.Xenotype.AllGenes.Contains(geneDef.prerequisite) &&
                !(addedGenes ?? []).Any(g => g.def == geneDef.prerequisite))
                return 0.0f;
            
            // No genes that conflict with genes in the pawn's xenotype or already added genes
            foreach (var gene in pawn.genes.Xenotype.AllGenes)
            {
                if (geneDef == gene)
                    return 0.0f;
                if (geneDef.exclusionTags != null && gene.exclusionTags != null &&
                    geneDef.exclusionTags.Union(gene.exclusionTags).Any())
                    return 0.0f;
            }
            foreach (var gene in addedGenes ?? [])
            {
                if (geneDef == gene.def)
                    return 0.0f;
                if (geneDef.exclusionTags != null && gene.def.exclusionTags != null &&
                    geneDef.exclusionTags.Union(gene.def.exclusionTags).Any())
                    return 0.0f;
            }

            return geneDef.selectionWeight;
        }

        public override void PostRemove()
        {
            base.PostRemove();

            if (DefExt.removeAfterAdding)
                return;
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