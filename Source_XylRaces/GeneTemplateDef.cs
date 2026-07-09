using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class GeneTemplateDef : Def
{
    public enum GeneTemplateType
    {
        PsychicAbility,
    }

    [UsedFromXml]
    public class AbilityBiostatInfo
    {
        public IntRange levels = new(0, int.MaxValue);
        public int biostatArc = 0;
        public int biostatCpx = 0;
        public int biostatMet = 0;
    }

    public string? iconPath;
    public Type geneClass = typeof(Gene);

    public required List<AbilityBiostatInfo> biostats;

    public GeneTemplateType geneTemplateType;

    public required GeneCategoryDef displayCategory;

    public int displayOrderOffset;

    public float selectionWeight = 1f;

    public override void ResolveReferences()
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        displayCategory ??= GeneCategoryDefOf.Miscellaneous;
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string item in base.ConfigErrors())
        {
            yield return item;
        }

        if (!typeof(Gene).IsAssignableFrom(geneClass))
            yield return "geneClass is not Gene or child thereof.";

        if (biostats is null)
            yield return $"{nameof(biostats)} is null";
    }
}
