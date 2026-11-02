// ReSharper disable UnusedParameter.Local

namespace Xylib;

/// <summary>
///     Marks the feature supported by a patch.
/// </summary>
/// <remarks>
///     <para>
///         This attribute is only for documentation and searchability; it has no runtime effect.
///     </para>
///     <para>
///         Prefer <see langword="typeof" /> or <see langword="nameof" /> over a bare string when passing the feature name.
///     </para>
/// </remarks>
/// <param name="featureName">
///     The name of the feature supported by the patch.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FeatureAttribute(string featureName) : Attribute
{
    /// <summary>
    ///     The name of the feature supported by the patch.
    /// </summary>
#pragma warning disable IDE0051
    public readonly string featureName = featureName;
#pragma warning restore IDE0051

    /// <summary>
    ///     Marks a patch as supporting the feature implemented by the specified type.
    /// </summary>
    /// <param name="feature">
    ///     The type that implements the supported feature.
    /// </param>
    public FeatureAttribute(Type feature) : this(feature.Name) { }
}

/// <summary>
///     Documents that a class is referenced by name from XML.
/// </summary>
/// <remarks>
///     <para>This attribute has no runtime effect.</para>
///     <para>
///         On an <see langword="abstract" /> class, this indicates that all subclasses should be marked with the
///         attribute.
///     </para>
/// </remarks>
[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class)]
public class UsedFromXmlAttribute : Attribute;

/// <summary>
///     Documents that a class, method, property, or field is loaded through reflection.
/// </summary>
/// <remarks>
///     This attribute has no runtime effect.
/// </remarks>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class UsedFromReflectionAttribute : Attribute;

/// <summary>
///     Marks a field as being required to be set in XML when the class is parsed as part of
///     loading game data.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RequiredAttribute : Attribute;
