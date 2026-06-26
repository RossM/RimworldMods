namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_UnlockRecipes : GeneCompProperties
{
    public List<RecipeDef> recipes;

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        yield return
            $"{"XylNewRecipes".Translate()}: {recipes.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
    }
}
