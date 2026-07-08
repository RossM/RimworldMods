namespace XylXenos;

[UsedFromXml]
public class GeneIngestionThoughtOverride
{
    public ThingDef thing;
    public List<ThoughtDef> thoughts;
    public FoodType allowedFoodTypes;
    public FoodType disallowedFoodTypes;
}

[UsedFromXml]
public class GeneCompProperties_IngestionThoughtOverrides : GeneCompProperties
{
    public List<GeneIngestionThoughtOverride> overrides;
}
