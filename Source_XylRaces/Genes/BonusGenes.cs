using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class BonusGenesInfo
    {
        public IntRange biostatArc = IntRange.Zero;
        public IntRange biostatCpx = new(int.MinValue, int.MaxValue);
        public IntRange biostatMet = new(int.MinValue, int.MaxValue);
        public float geneChance = 1.0f;
        [CanBeNull] public List<GeneDef> allowedGenes;
        [CanBeNull] public List<GeneDef> prohibitedGenes;
        public bool removeAfterAdding = false;
        public bool ignoreSelectionWeight = false;
        public IntRange count = IntRange.One;
    }

    [UsedImplicitly]
    public class BonusGenes : GeneExt
    {
        [NotNull]
        public BonusGenesInfo BonusGenesInfo => DefExt.bonusGenes!;

        public List<Gene> addedGenes = [];

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref addedGenes, nameof(addedGenes), LookMode.Reference);
        }

        public override void PostAdd()
        {
            base.PostAdd();

            if (!Rand.Chance(BonusGenesInfo.geneChance))
                return;

            int count = BonusGenesInfo.count.RandomInRange;

            for (int i = 0; i < count; i++)
            {
                List<GeneDef> genes = !BonusGenesInfo.allowedGenes.NullOrEmpty()
                    ? BonusGenesInfo.allowedGenes
                    : DefDatabase<GeneDef>.AllDefsListForReading;
                if (genes.TryRandomElementByWeight(GeneWeight, out GeneDef geneDef))
                    AddGene(geneDef);
            }

            if (BonusGenesInfo.removeAfterAdding)
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

            addedGenes.Add(pawn.genes.AddGene(geneDef, GeneType == GeneType.Xenogene));
        }

        private float GeneWeight(GeneDef geneDef)
        {
            if (!geneDef.canGenerateInGeneSet)
                return 0.0f;

            if (geneDef.modContentPack != null && Config.Instance.ignoreGenesFromMods.Contains(geneDef.modContentPack.PackageId))
                return 0.0f;

            if (!BonusGenesInfo.prohibitedGenes.NullOrEmpty() && BonusGenesInfo.prohibitedGenes.Contains(geneDef))
                return 0.0f;

            if (!BonusGenesInfo.biostatArc.Includes(geneDef.biostatArc))
                return 0.0f;
            if (!BonusGenesInfo.biostatCpx.Includes(geneDef.biostatCpx))
                return 0.0f;
            if (!BonusGenesInfo.biostatMet.Includes(geneDef.biostatMet))
                return 0.0f;

            var defExt = geneDef.DefExt;
            if (defExt != null)
            {
                if (defExt.gender != null && defExt.gender != pawn.gender)
                    return 0.0f;
                if (defExt.geneType != null && defExt.geneType != GeneType)
                    return 0.0f;
            }

            // Aptitude-giving genes must not apply to only disabled skills
            if (!geneDef.aptitudes.NullOrEmpty() && geneDef.aptitudes.All(aptitude => pawn.skills.GetSkill(aptitude.skill).TotallyDisabled))
                return 0.0f;

            // No genes with requirements, unless they are met by the pawn's xenotype or already added genes
            if (geneDef.prerequisite != null && !pawn.genes.Xenotype.AllGenes.Contains(geneDef.prerequisite) &&
                !addedGenes.Any(g => g.def == geneDef.prerequisite))
            {
                return 0.0f;
            }

            // No genes that conflict with genes in the pawn's xenotype or already added genes
            foreach (var gene in pawn.genes.Xenotype.AllGenes)
            {
                if (geneDef == gene)
                    return 0.0f;

                if (geneDef.exclusionTags != null && gene.exclusionTags != null &&
                    geneDef.exclusionTags.Intersect(gene.exclusionTags).Any())
                {
                    return 0.0f;
                }
            }

            foreach (var gene in addedGenes)
            {
                if (geneDef == gene.def)
                    return 0.0f;

                if (geneDef.exclusionTags != null && gene.def.exclusionTags != null &&
                    geneDef.exclusionTags.Intersect(gene.def.exclusionTags).Any())
                {
                    return 0.0f;
                }
            }

            return BonusGenesInfo.ignoreSelectionWeight ? 1.0f : geneDef.selectionWeight;
        }

        public override void PostRemove()
        {
            base.PostRemove();

            if (BonusGenesInfo.removeAfterAdding)
                return;
            if (addedGenes == null)
                return;

            foreach (var gene in addedGenes)
                pawn.genes.RemoveGene(gene);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismChanceLabel".TranslateSimple(),
                BonusGenesInfo.geneChance.ToStringPercent(), "XylAtavismChanceDesc".TranslateSimple(), 1002);
            if (addedGenes == null)
                yield break;
            string text = string.Join(", ", addedGenes.Select(g => g.Label)).CapitalizeFirst();
            yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylAtavismGenesLabel".TranslateSimple(),
                text, "XylAtavismGenesDesc".TranslateSimple(), 1001);
        }
    }
}
