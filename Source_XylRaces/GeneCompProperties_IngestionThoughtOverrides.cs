namespace XylXenos;

[UsedFromXml]
public class GeneIngestionThoughtOverride
{
    public ThingDef thing;
    public List<ThoughtDef> thoughts;
    [CanBeNull] public List<FoodGroupDef> allowedFoodGroups;
    [CanBeNull] public List<FoodGroupDef> disallowedFoodGroups;
}

[UsedFromXml]
public class GeneCompProperties_IngestionThoughtOverrides : GeneCompProperties
{
    public List<GeneIngestionThoughtOverride> overrides;
}
