using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Hediff_DietDependency : Hediff_Genetic
    {
        public bool ShouldSatisfy => Severity >= def.stages[2].minSeverity - 0.5f;

        public new Gene_DietDependency Gene => (Gene_DietDependency)base.Gene;

        public float SeverityReductionPerNutrition => Gene.DefExt.severityReductionPerNutrition;
    }
}
