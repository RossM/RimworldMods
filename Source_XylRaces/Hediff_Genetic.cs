using System.Linq;
using Verse;

namespace XylXenos
{
    public interface IGene_HediffSource
    {
        bool CausesHediff(HediffDef hediffDef);
    }

    public class Hediff_Genetic : HediffWithComps
    {
        public override bool ShouldRemove => Gene is not { Active: true };

        public Gene Gene => cachedGene ??= (Gene)pawn.GenesOfType<IGene_HediffSource>().FirstOrDefault(gene => gene.CausesHediff(def));
        [Unsaved] private Gene cachedGene;

        public override float Severity
        {
            get => Gene is not { Active: true } ? def.initialSeverity : base.Severity;
            set => base.Severity = value;
        }
    }
}
