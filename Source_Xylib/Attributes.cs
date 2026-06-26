// ReSharper disable UnusedParameter.Local

namespace Xylib;

/// <summary>
/// This attribute is used to mark which feature a patch is supporting. It is purely to help organize patches and make searching for
/// the code supporting a specific feature easier. It has no runtime effect.
/// <br/><br/>
/// It is recommended to use <c>typeof</c> or <c>nameof</c> for the parameter rather than a bare string.
/// </summary>
/// <param name="featureName">The name of the feature this patch supports. It is purely for documentation and has no runtime effect.</param>

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FeatureAttribute(string featureName) : Attribute
{
    // ReSharper disable once UnusedMember.Global
    public readonly string featureName = featureName;

    /// <summary>
    /// This constructor allows you to specify a type that implements the feature this patch supports. It is purely for documentation and has no runtime effect.
    /// </summary>
    /// <param name="feature">A type implementing the feature this patch supports. It is purely for documentation and has no runtime effect.</param>
    public FeatureAttribute(Type feature) : this(feature.Name)
    {
    }
}

/// <summary>
/// This attribute documents that the class is referred to by name from XML. It has no runtime effect.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class UsedFromXmlAttribute : Attribute;

/// <summary>
/// This attribute documents that a class, method, property, or field is loaded via reflection. It has no runtime effect.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class UsedFromReflectionAttribute : Attribute;

/// <summary>
/// When applied to a class, this causes the class to be treated as a def generator. The library will look for a static method
/// named <c>ImpliedDefs</c> that takes a <see cref="bool"/> and returns a value convertible to <see cref="IEnumerable{T}"/> where
/// <c>typeof(T)</c> is <c>defType</c>. It will run the method after the base game def generators are run, and add
/// the returned defs to <see cref="DefDatabase{T}"/>.
/// </summary>
/// <param name="defType">The subclass of <see cref="Def"/> that the <c>ImpliedDefs</c> function generates, such as <see cref="ThingDef"/>
/// or <see cref="GeneDef"/>.</param>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class DefGeneratorAttribute(Type defType) : Attribute
{
    public readonly Type defType = defType;
}
