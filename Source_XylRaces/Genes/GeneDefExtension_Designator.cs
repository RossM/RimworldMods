using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_Designator : GeneDefExtension
    {
        public List<BuildableDef> addDesignators;

        protected override IEnumerable<string> GetCustomEffectDescriptions()
        {
            if (addDesignators.NullOrEmpty())
                yield break;

            yield return "XylNewBuildings".Translate() + ": " +
                         addDesignators.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList();
        }
    }
}
