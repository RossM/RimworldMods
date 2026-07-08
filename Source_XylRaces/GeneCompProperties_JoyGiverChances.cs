using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace XylXenos;

public class JoyGiverFactor
{
    public required JoyGiverDef joyGiver;
    public float factor = 1.0f;

    [UsedFromReflection]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "joyGiver", xmlRoot.Name);
        factor = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }
}

[UsedFromXml]
public class GeneCompProperties_JoyGiverChances : GeneCompProperties
{
    public required List<JoyGiverFactor> factors;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (factors is null)
        {
            yield return $"{nameof(factors)} is null";
            yield break;
        }

        foreach (var factor in factors)
        {
            if (factor.joyGiver is null)
                yield return $"null {nameof(factor.joyGiver)} in {nameof(factors)}";
        }
    }
}
