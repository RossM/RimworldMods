using System.Linq;
using Verse;
using XylXenos.Genes;

namespace XylXenos
{
    public class Hediff_Genetic : HediffWithComps
    {
        public override bool ShouldRemove => Gene is not { Active: true };

        public GeneExt Gene => cachedGene ??= pawn.GenesOfType<GeneExt>().FirstOrDefault(gene => gene.CausesHediff(def));
        [Unsaved] private GeneExt cachedGene;

        public override float Severity
        {
            get => Gene is not { Active: true } ? def.initialSeverity : base.Severity;
            set => base.Severity = value;
        }
    }
}
