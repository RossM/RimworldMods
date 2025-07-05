using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_GenderRatio : DefModExtension
    {
        public float femaleChance = 0.5f;
    }

    [UsedImplicitly]
    public class Gene_GenderRatio : Gene
    {
        public GeneDefExtension_GenderRatio DefExt => def.GetModExtension<GeneDefExtension_GenderRatio>();

        public override bool Active => base.Active && pawn.genes.HasEndogene(def);

        public Gender GetGender()
        {
            return Rand.Chance(DefExt.femaleChance) ? Gender.Female : Gender.Male;
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            string text = "XylGenderRatioValue".Translate(DefExt.femaleChance.ToStringPercent(),
                (1 - DefExt.femaleChance).ToStringPercent());
            yield return new StatDrawEntry(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
                text, "XylGenderRatioDesc".TranslateSimple(), 1);
        }
    }
}
