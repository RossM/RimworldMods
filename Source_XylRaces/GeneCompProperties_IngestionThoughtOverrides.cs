using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class GeneIngestionThoughtOverride
{
    public ThingDef? thing;
    public required List<ThoughtDef> thoughts;
    public List<FoodGroupDef>? allowedFoodGroups;
    public List<FoodGroupDef>? disallowedFoodGroups;
}

[UsedFromXml]
public class GeneCompProperties_IngestionThoughtOverrides : GeneCompProperties
{
    public required List<GeneIngestionThoughtOverride> overrides;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (overrides is null)
        {
            yield return $"{nameof(overrides)} is null";
            yield break;
        }

        foreach (var o in overrides)
        {
            if (o.thoughts is null)
                yield return $"null {nameof(o.thoughts)} in {nameof(overrides)}";
        }
    }
}
