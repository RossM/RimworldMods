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
    public class XenotypeDefExtension : DefModExtension
    {
        public List<MemeDef> agreeingMemes;
        public List<MemeDef> disagreeingMemes;
    }
}
