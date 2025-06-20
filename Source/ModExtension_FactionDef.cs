using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_FactionDef : DefModExtension
    {
        public List<BiomeDef> allowedBiomes;
        public List<Hilliness> allowedHilliness;
        public bool waterRequired = false;
    }
}
