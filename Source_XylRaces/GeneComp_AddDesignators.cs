namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_AddDesignators : GeneCompProperties
{
    public List<BuildableDef> buildables;

    public GeneCompProperties_AddDesignators()
    {
        compClass = typeof(GeneComp_AddDesignators);
    }

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        yield return
            $"{"XylNewBuildings".Translate()}: {buildables.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
    }
}

public class GeneComp_AddDesignators : GeneComp
{
    public GeneCompProperties_AddDesignators Props => (GeneCompProperties_AddDesignators)props;
}
