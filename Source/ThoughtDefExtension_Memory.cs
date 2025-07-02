using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThoughtDefExtension_Memory : DefModExtension
    {
        public List<ThoughtDef> extraThoughts;
    }
}
