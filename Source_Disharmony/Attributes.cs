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
public class InnerPrefixAttribute : PatchTypeAttribute
{
    public InnerPrefixAttribute(Type type, string memberName, Type[]? parameterTypes = null, Type[]? genericTypes = null) :
        base(PatchType.InnerPrefix, type, memberName, MemberType.Any, parameterTypes, genericTypes) { }

    public InnerPrefixAttribute(
        Type type,
        string memberName,
        MemberType memberType,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null) :
        base(PatchType.InnerPrefix, type, memberName, memberType, parameterTypes, genericTypes) { }
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPostfixAttribute : PatchTypeAttribute
{
    public InnerPostfixAttribute(Type type, string memberName, Type[]? parameterTypes = null, Type[]? genericTypes = null) :
        base(PatchType.InnerPostfix, type, memberName, MemberType.Any, parameterTypes, genericTypes) { }

    public InnerPostfixAttribute(
        Type type,
        string memberName,
        MemberType memberType,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null) :
        base(PatchType.InnerPostfix, type, memberName, memberType, parameterTypes, genericTypes) { }
}

/// <summary>
///     This attribute causes the infix transpiler to log the instruction sequence of the modified method to the debug log.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class DebugAttribute : Attribute;

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TargetAttribute(
    Type? type,
    string methodName,
    MemberType memberType,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : Attribute
{
    public readonly Type? type = type;
    public readonly string methodName = methodName;
    public readonly MemberType memberType = memberType;
    public readonly Type[]? parameterTypes = parameterTypes;
    public readonly Type[]? genericTypes = genericTypes;

    public TargetAttribute(string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : this(null, methodName, MemberType.Any, parameterTypes, genericTypes) { }

    public TargetAttribute(string methodName, MemberType memberType, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    public TargetAttribute(Type? type, string methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes, null) { }

    public TargetAttribute(Type? type, string methodName, MemberType memberType, params Type[] parameterTypes)
        : this(type, methodName, memberType, parameterTypes, null) { }

    public TargetAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes, null) { }

    public TargetAttribute(string methodName, MemberType memberType, params Type[] parameterTypes)
        : this(null, methodName, memberType, parameterTypes, null) { }

    public TargetAttribute(Type? type, string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : this(type, methodName, MemberType.Any, parameterTypes, genericTypes) { }
}

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TargetsAttribute : TargetAttribute
{
    public TargetsAttribute(string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : base(methodName, parameterTypes, genericTypes) { }

    public TargetsAttribute(string methodName, MemberType memberType, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : base(methodName, memberType, parameterTypes, genericTypes) { }

    public TargetsAttribute(Type? type, string methodName, params Type[] parameterTypes)
        : base(type, methodName, parameterTypes) { }

    public TargetsAttribute(Type? type, string methodName, MemberType memberType, params Type[] parameterTypes)
        : base(type, methodName, memberType, parameterTypes) { }

    public TargetsAttribute(string methodName, params Type[] parameterTypes)
        : base(methodName, parameterTypes) { }

    public TargetsAttribute(string methodName, MemberType memberType, params Type[] parameterTypes)
        : base(methodName, memberType, parameterTypes) { }

    public TargetsAttribute(Type? type, string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
        : base(type, methodName, parameterTypes, genericTypes) { }

    public TargetsAttribute(
        Type? type,
        string methodName,
        MemberType memberType,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : base(type, methodName, memberType, parameterTypes, genericTypes) { }
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
public class ReturnAttribute() : ParameterBindingAttribute(Scope.Any);

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class StateAttribute() : ParameterBindingAttribute(Scope.Outer);

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class FieldAttribute(string? name, Scope scope = Scope.Any) : ParameterBindingAttribute(scope)
{
    public readonly string? name = name;

    public FieldAttribute(Scope scope = Scope.Any) : this(null, scope) { }
}
