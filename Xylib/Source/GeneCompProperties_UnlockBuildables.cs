namespace Xylib;

[UsedFromXml]
public class GeneCompProperties_UnlockBuildables : GeneCompProperties
{
    public required List<BuildableDef> buildables;

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        yield return
            $"{"XylNewBuildings".Translate()}: {buildables.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
    }
}
