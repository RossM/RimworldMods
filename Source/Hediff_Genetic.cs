using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{

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
                    foreach (Gene t in genesListForReading)
                    {
                        if (t is Gene_DietDependency gene_dietDependency)
                        {
                            if (gene_dietDependency.DefExt?.hediffDef == def)
                            {
                                cachedGene = gene_dietDependency;
                                break;
                            }
                        }
                        else if (t is Gene_Hediff gene_hediff)
                        {
                            if (gene_hediff.DefExt?.hediffGivers.Any(g => g.hediff == def) ?? false)
                            {
                                cachedGene = gene_hediff;
                                break;
                            }
                        }
                    }
                }
                return cachedGene;
            }
        }

        public override float Severity
        {
            get
            {
                if (LinkedGene == null || !LinkedGene.Active)
                {
                    return def.initialSeverity;
                }
                return base.Severity;
            }
            set
            {
                base.Severity = value;
            }
        }
    }
}
