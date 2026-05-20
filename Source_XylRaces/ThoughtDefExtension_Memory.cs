using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class ThoughtDefExtension_Memory : DefModExtension
    {
        public List<ThoughtDef> extraThoughts;
    }
}
