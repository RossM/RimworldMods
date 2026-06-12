// ReSharper disable UnusedParameter.Local

namespace XylXenos;
// This attribute serves as documentation of which patches are to support which parts of the mod. It
// has no actual effect.

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FeatureAttribute(string featureName) : Attribute
{
    // ReSharper disable once UnusedMember.Global
    public readonly string featureName = featureName;

    public FeatureAttribute(Config.Feature feature) : this(feature.ToString())
    {
    }

    public FeatureAttribute(Type feature) : this(feature.Name)
    {
    }
}

[MeansImplicitUse]
public class UsedFromXmlAttribute : Attribute;

[MeansImplicitUse]
public class UsedFromReflectionAttribute : Attribute;

public class GeneClassAttribute(Type geneClass) : Attribute
{
    public readonly Type geneClass = geneClass;
}

[MeansImplicitUse]
public class DefGeneratorAttribute(Type defType) : Attribute
{
    public readonly Type defType = defType;
}
