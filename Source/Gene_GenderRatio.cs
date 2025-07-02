using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class GeneDefExtension_GenderRatio : DefModExtension
    {
        public float femaleChance = 0.5f;
    }

    // ReSharper disable once UnusedMember.Global
    public class Gene_GenderRatio : Gene
    {
        public GeneDefExtension_GenderRatio DefExt => def.GetModExtension<GeneDefExtension_GenderRatio>();

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
