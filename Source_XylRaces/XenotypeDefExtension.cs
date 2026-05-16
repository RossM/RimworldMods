using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class XenotypeDefExtension : DefModExtension
    {
        public bool allowSolidBackstories = true;
        public List<MemeDef> agreeingMemes;
        public List<MemeDef> disagreeingMemes;
    }
}
