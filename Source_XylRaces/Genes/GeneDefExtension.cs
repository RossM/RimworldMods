using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension : DefModExtension
    {
        public IEnumerable<string> CustomEffectDescriptions =>
            customEffectDescriptions ??= GetCustomEffectDescriptions().ToList();

        [Unsaved] private List<string> customEffectDescriptions;

        protected virtual IEnumerable<string> GetCustomEffectDescriptions()
        {
            return Enumerable.Empty<string>();
        }
    }
}
