using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ThingDefExtension_WetnessSource : DefModExtension
    {
        public float wetnessLevel = 1.0f;
        public EffecterDef effecter;
    }
}
