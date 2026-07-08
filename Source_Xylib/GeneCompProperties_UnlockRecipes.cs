namespace Xylib;

[UsedFromXml]
public class GeneCompProperties_UnlockRecipes : GeneCompProperties
{
    public required List<RecipeDef> recipes;

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        yield return
            $"{"XylNewRecipes".Translate()}: {recipes.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (recipes is null)
            yield return $"{nameof(recipes)} is null";
    }
}
