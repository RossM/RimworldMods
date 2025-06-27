using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{ 
    public interface IGene_HediffSource
    {
        bool CausesHediff(HediffDef hediffDef);
    }

    public class Hediff_Genetic : HediffWithComps
    {
        [Unsaved(false)]
        private Gene cachedGene;

        public override bool ShouldRemove => LinkedGene is not { Active: true };

        public Gene LinkedGene => cachedGene ??=
            pawn.genes?.GenesListForReading.FirstOrDefault(gene =>
                gene is IGene_HediffSource hediffSource && hediffSource.CausesHediff(def));

        public override float Severity
        {
            get => LinkedGene is not { Active: true } ? def.initialSeverity : base.Severity;
            set => base.Severity = value;
        }
    }
}
