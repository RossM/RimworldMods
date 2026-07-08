namespace XylXenos;

[UsedFromXml]
public class GeneIngestionThoughtOverride
{
    public ThingDef thing;
    public List<ThoughtDef> thoughts;
    [CanBeNull] public List<FoodDef> allowedFoodTypes;
    [CanBeNull] public List<FoodDef> disallowedFoodTypes;
}

[UsedFromXml]
public class GeneCompProperties_IngestionThoughtOverrides : GeneCompProperties
{
    public List<GeneIngestionThoughtOverride> overrides;
}
