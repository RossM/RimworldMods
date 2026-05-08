using System.Collections.Generic;
using Verse;

namespace XylRacesCore
{
    public class ChemicalDefExtension : DefModExtension
    {
        public List<GeneDef> requiredGenesAll;
        public List<GeneDef> requiredGenesAny;
        public List<GeneDef> prohibitedGenes;
    }
}
