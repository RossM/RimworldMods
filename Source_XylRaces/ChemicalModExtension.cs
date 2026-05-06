using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ChemicalModExtension : DefModExtension
    {
        public List<GeneDef> requiredGenesAny;
        public List<GeneDef> prohibitedGenes;
    }
}
