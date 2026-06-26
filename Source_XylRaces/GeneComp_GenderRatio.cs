namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_GenderRatio : GeneCompProperties
{
    public float femaleChance = 0.5f;

    public string GenderRatioDescription =>
        femaleChance switch
        {
            >= 1.0f => "XylGenderRatioAlwaysFemale".Translate(),
            <= 0.0f => "XylGenderRatioAlwaysMale".Translate(),
            { } chance => "XylGenderRatioValue".Translate(chance.ToStringPercent(),
                (1 - chance).ToStringPercent())
        };


    public GeneCompProperties_GenderRatio()
    {
        compClass = typeof(GeneComp_GenderRatio);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
            GenderRatioDescription, "XylGenderRatioDesc".TranslateSimple(), 1);
    }
}

public class GeneComp_GenderRatio : GeneComp
{
    public GeneCompProperties_GenderRatio Props => (GeneCompProperties_GenderRatio)props;
}
