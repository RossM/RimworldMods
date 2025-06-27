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

        public override bool ShouldRemove
        {
            get
            {
                if (LinkedGene != null)
                {
                    return !LinkedGene.Active;
                }
                return true;
            }
        }

        public Gene LinkedGene
        {
            get
            {
                if (cachedGene == null && pawn.genes != null)
                {
                    List<Gene> genesListForReading = pawn.genes.GenesListForReading;
                    foreach (Gene gene in genesListForReading)
                    {
                        if (gene is IGene_HediffSource hediffSource && hediffSource.CausesHediff(def))
                        {
                            cachedGene = gene;
                            break;
                        }
                    }
                }
                return cachedGene;
            }
        }

        public override float Severity
        {
            get => LinkedGene is not { Active: true } ? def.initialSeverity : base.Severity;
            set => base.Severity = value;
        }
    }
}
