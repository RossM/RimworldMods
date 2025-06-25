using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_Atavism : DefModExtension
    {
        public int biostatArcMin, biostatArcMax;
        public int biostatCpxMin, biostatCpxMax;
        public int biostatMetMin, biostatMetMax;
        public float geneChance = 1.0f;
        public float extraGeneChance = 1.0f;
        public List<GeneDef> extraGenes;
    }
}
