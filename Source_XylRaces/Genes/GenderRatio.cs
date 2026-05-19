using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneDefExtension_GenderRatio : GeneDefExtension
    {
        public float femaleChance = 0.5f;

        public Gender GetGender()
        {
            return Rand.Chance(femaleChance) ? Gender.Female : Gender.Male;
        }

        public string GetGenderRatioDescription()
        {
            return femaleChance switch
            {
                >= 1.0f => "XylGenderRatioAlwaysFemale".Translate(),
                <= 0.0f => "XylGenderRatioAlwaysMale".Translate(),
                _ => "XylGenderRatioValue".Translate(femaleChance.ToStringPercent(),
                    (1 - femaleChance).ToStringPercent())
            };
        }

        protected override IEnumerable<string> GetCustomEffectDescriptions()
        {
            yield return $"{"XylGenderRatioLabel".TranslateSimple()}: {GetGenderRatioDescription()}";
        }

        protected override IEnumerable<StatDrawEntry> GetSpecialDisplayStats()
        {
            yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
                GetGenderRatioDescription(), "XylGenderRatioDesc".TranslateSimple(), 1);
        }
    }

    [UsedImplicitly]
    public class GenderRatio : Gene
    {
        public GeneDefExtension_GenderRatio DefExt => def.GetModExtension<GeneDefExtension_GenderRatio>();

        public override bool Active => base.Active && pawn.genes.HasEndogene(def);
    }
}
