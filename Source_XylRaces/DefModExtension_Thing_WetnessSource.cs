using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Thing_WetnessSource : DefModExtension
{
    public float wetnessLevel = 1.0f;
    public required JobDef job;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (job is null)
            yield return $"{nameof(job)} is null";
    }
}
