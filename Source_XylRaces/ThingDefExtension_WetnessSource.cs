using JetBrains.Annotations;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class ThingDefExtension_WetnessSource : DefModExtension
    {
        public float wetnessLevel = 1.0f;
        public EffecterDef effecter;
    }
}
