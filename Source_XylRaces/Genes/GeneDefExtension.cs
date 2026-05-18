using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneDefExtension : DefModExtension
    {
        public IEnumerable<string> CustomEffectDescriptions =>
            customEffectDescriptions ??= GetCustomEffectDescriptions().ToList();

        public IEnumerable<StatDrawEntry> SpecialDisplayStats =>
            specialDisplayStats ??= GetSpecialDisplayStats().ToList();

        [Unsaved] private List<string> customEffectDescriptions;
        [Unsaved] private List<StatDrawEntry> specialDisplayStats;

        protected virtual IEnumerable<string> GetCustomEffectDescriptions()
        {
            return Enumerable.Empty<string>();
        }

        protected virtual IEnumerable<StatDrawEntry> GetSpecialDisplayStats()
        {
            return Enumerable.Empty<StatDrawEntry>();
        }
    }
}
