using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension : DefModExtension
    {
        [Unsaved] private List<string> customEffectDescriptions;

        public IEnumerable<string> CustomEffectDescriptions =>
            customEffectDescriptions ??= GetCustomEffectDescriptions().ToList();

        protected virtual IEnumerable<string> GetCustomEffectDescriptions()
        {
            return Enumerable.Empty<string>();
        }
    }
}
