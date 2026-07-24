using JetBrains.Annotations;

namespace Disharmony;

[PublicAPI]
public static class Ref<T>;

[PublicAPI]
public static class In<T>;

[PublicAPI]
public static class Out<T>;

[PublicAPI]
public enum MemberType
{
    /// <summary>
    ///     Matches methods, properties, and fields. For properties, applies to the property getter.
    /// </summary>
    Any,

    /// <summary>
    ///     Matches methods.
    /// </summary>
    Method,

    /// <summary>
    ///     Matches properties and fields. For properties, applies to the property getter.
    /// </summary>
    Getter,

    /// <summary>
    ///     Matches properties and fields. For properties, applies to the property setter. For fields, throws a
    ///     <see cref="NotSupportedException" />.
    /// </summary>
    Setter,

    /// <summary>
    ///     Matches constructors.
    /// </summary>
    Constructor,
}

public enum Scope
{
    /// <summary>
    ///     Represents access to parameters or results of either inner or outer method.
    /// </summary>
    /// <remarks>
    ///     If both the inner and outer methods have a matching parameter, it matches the inner parameter.
    /// </remarks>
    Any,

    /// <summary>
    ///     Represents access to parameters or results of the inner method.
    /// </summary>
    Inner,

    /// <summary>
    ///     Represents access to parameters or results of the outer method.
    /// </summary>
    Outer,
}

public abstract class PatchTypeAttribute(
    PatchType patchType,
    Type? type = null,
    string? memberName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null) : Attribute
{
    public readonly PatchType patchType = patchType;
    public readonly Type? type = type;
    public readonly string? memberName = memberName;
    public readonly MemberType memberType = memberType;
    public readonly Type[]? parameterTypes = parameterTypes;
    public readonly Type[]? genericTypes = genericTypes;
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PrefixAttribute() : PatchTypeAttribute(PatchType.Prefix);

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PostfixAttribute() : PatchTypeAttribute(PatchType.Postfix);

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPrefixAttribute(
    Type type,
    string? memberName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : PatchTypeAttribute(PatchType.InnerPrefix, type, memberName, memberType, parameterTypes, genericTypes)
{
    public InnerPrefixAttribute(Type type, string? memberName) : this(type, memberName, MemberType.Any) { }

    public InnerPrefixAttribute(Type type, string? memberName, params Type[] parameterTypes) : this(type, memberName, MemberType.Any,
        parameterTypes)
    { }
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPostfixAttribute(
    Type type,
    string? memberName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : PatchTypeAttribute(PatchType.InnerPostfix, type, memberName, memberType, parameterTypes, genericTypes)
{
    public InnerPostfixAttribute(Type type, string? memberName) : this(type, memberName, MemberType.Any)
    { }

    public InnerPostfixAttribute(Type type, string? memberName, params Type[] parameterTypes) : this(type, memberName, MemberType.Any,
        parameterTypes) { }
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPostfixConstantAttribute : PatchTypeAttribute
{
    public readonly object value;

    public InnerPostfixConstantAttribute(int value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    public InnerPostfixConstantAttribute(long value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    public InnerPostfixConstantAttribute(float value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    public InnerPostfixConstantAttribute(double value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    public InnerPostfixConstantAttribute(string value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }
}

/// <summary>
///     This attribute causes the infix transpiler to log the modified IL and, when available, the Mono JIT assembly to the
///     Harmony debug log.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class DebugAttribute : Attribute;

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TargetAttribute(
    Type? type,
    string? methodName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : Attribute
{
    public readonly Type? type = type;
    public readonly string? methodName = methodName;
    public readonly MemberType memberType = memberType;
    public readonly Type[]? parameterTypes = parameterTypes;
    public readonly Type[]? genericTypes = genericTypes;

    public TargetAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    public TargetAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    public TargetAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    public TargetAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    public TargetAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TargetsAttribute(
    Type? type,
    string? methodName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : TargetAttribute(type, methodName, memberType, parameterTypes, genericTypes)
{
    public TargetsAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    public TargetsAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    public TargetsAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    public TargetsAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    public TargetsAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class InlineAttribute : Attribute;

public abstract class ParameterBindingAttribute(Scope scope) : Attribute
{
    public readonly Scope scope = scope;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ParameterAttribute : ParameterBindingAttribute
{
    public readonly int? index = null;
    public readonly string? name = null;

    public ParameterAttribute(Scope scope = Scope.Any) : base(scope) { }

    public ParameterAttribute(string? name, Scope scope = Scope.Any) : base(scope)
    {
        this.name = name;
    }

    public ParameterAttribute(int index, Scope scope = Scope.Any) : base(scope)
    {
        this.index = index;
    }
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class InstanceAttribute(Scope scope = Scope.Any) : ParameterBindingAttribute(scope);

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ReturnValueAttribute() : ParameterBindingAttribute(Scope.Any);

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class StateAttribute(string? key) : ParameterBindingAttribute(Scope.Outer)
{
    public readonly string? key = key;

    public StateAttribute() : this(null) { }
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class FieldAttribute(string? name, Scope scope = Scope.Any) : ParameterBindingAttribute(scope)
{
    public readonly string? name = name;

    public FieldAttribute(Scope scope = Scope.Any) : this(null, scope) { }
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class BaseMethodAttribute() : ParameterBindingAttribute(Scope.Outer);
