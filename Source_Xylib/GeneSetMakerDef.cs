namespace Xylib;

[UsedFromXml]
public class GeneSetMakerDef : Def
{
    public required GeneSetMaker root;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        root.ResolveReferences();
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (root == null)
        {
            yield return $"{nameof(root)} is null";
            yield break;
        }

        foreach (var error in root.ConfigErrors())
            yield return error;
    }
}
