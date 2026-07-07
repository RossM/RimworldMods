namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_GenderRatio : GeneCompProperties
{
    public string GenderRatioDescription =>
        femaleChance switch
        {
            >= 1.0f => "XylGenderRatioAlwaysFemale".Translate(),
            <= 0.0f => "XylGenderRatioAlwaysMale".Translate(),
            var chance => "XylGenderRatioValue".Translate(chance.ToStringPercent(),
                (1 - chance).ToStringPercent())
        };

    public float femaleChance = 0.5f;


    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
            GenderRatioDescription, "XylGenderRatioDesc".TranslateSimple(), 1);
    }
}
