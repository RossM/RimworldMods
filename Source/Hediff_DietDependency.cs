using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    internal class Hediff_DietDependency : Hediff_Genetic
    {
        public bool ShouldSatisfy => Severity >= def.stages[2].minSeverity - 0.5f;

        [Unsaved(false)]
        private Gene_DietDependency cachedGene;

        public Gene_DietDependency Gene
        {
            get
            {
                if (cachedGene == null)
                {
                    cachedGene = pawn.FirstGeneOfType<Gene_DietDependency>(g => g.DefExt?.hediffDef == def);
                }

                return cachedGene;
            }
        }
    }
}
