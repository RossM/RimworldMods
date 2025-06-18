using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XylRacesCore
{
    internal class Hediff_DietDependency : Hediff_Genetic
    {
        public bool ShouldSatisfy => Severity >= def.stages[2].minSeverity - 0.2f;
    }
}
