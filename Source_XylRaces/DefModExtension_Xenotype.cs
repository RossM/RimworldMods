using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class DefModExtension_Xenotype : DefModExtension
    {
        public bool allowSolidBackstories = true;
        public List<MemeDef> agreeingMemes;
        public List<MemeDef> disagreeingMemes;
    }
}
