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

/// <summary>
///     Marks a patch method to run before each member selected by <see cref="TargetAttribute" /> or
///     <see cref="TargetsAttribute" />. A prefix that returns <see langword="false" /> skips the selected member.
/// </summary>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PrefixAttribute() : PatchTypeAttribute(PatchType.Prefix);

/// <summary>
///     Marks a patch method to run after each member selected by <see cref="TargetAttribute" /> or
///     <see cref="TargetsAttribute" />. Use <see cref="ReturnValueAttribute" /> on a patch parameter to inspect or replace
///     the member's return value.
/// </summary>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PostfixAttribute() : PatchTypeAttribute(PatchType.Postfix);

/// <summary>
///     Marks a patch method to run before every matching inner member access or call made by the selected outer methods.
///     A prefix that returns <see langword="false" /> skips the matching inner member.
/// </summary>
/// <param name="type">
///     The type that declares the inner member, or <see langword="null" /> to resolve it from
///     <paramref name="memberName" />.
/// </param>
/// <param name="memberName">
///     The name of the inner member to match, or <see langword="null" /> when matching a constructor.
/// </param>
/// <param name="memberType">The kind of member access or call to match.</param>
/// <param name="parameterTypes">
///     The parameter types used to select an overload, or <see langword="null" /> to match without a parameter signature.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used to select a constructed generic method, or <see langword="null" /> when not
///     selecting one.
/// </param>
/// <remarks>
///     Member lookup supports a type-qualified name in the form <c>TypeName:MemberName</c>. When <paramref name="type" />
///     is <see langword="null" />, a qualified type name can instead be included as the leading portion of
///     <paramref name="memberName" />. Dotted names can traverse nested types, select a local function as
///     <c>OuterMethod.LocalFunction</c>, or select compiler-generated lambdas as <c>OuterMethod.*</c>. Only members declared
///     directly by the resolved type are considered. In <paramref name="parameterTypes" />, use <see cref="Ref{T}" />,
///     <see cref="In{T}" />, or <see cref="Out{T}" /> to match <see langword="ref" />, <see langword="in" />, or
///     <see langword="out" /> parameters.
/// </remarks>
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
    /// <summary>
    ///     Runs the patch before each access or call to the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    public InnerPrefixAttribute(Type type, string? memberName) : this(type, memberName, MemberType.Any) { }

    /// <summary>
    ///     Runs the patch before each call to the specified overload of the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public InnerPrefixAttribute(Type type, string? memberName, params Type[] parameterTypes) : this(type, memberName, MemberType.Any,
        parameterTypes)
    { }
}

/// <summary>
///     Marks a patch method to run after every matching inner member access or call made by the selected outer methods.
///     Use <see cref="ReturnValueAttribute" /> on a patch parameter to inspect or replace the inner member's result.
/// </summary>
/// <param name="type">
///     The type that declares the inner member, or <see langword="null" /> to resolve it from
///     <paramref name="memberName" />.
/// </param>
/// <param name="memberName">
///     The name of the inner member to match, or <see langword="null" /> when matching a constructor.
/// </param>
/// <param name="memberType">The kind of member access or call to match.</param>
/// <param name="parameterTypes">
///     The parameter types used to select an overload, or <see langword="null" /> to match without a parameter signature.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used to select a constructed generic method, or <see langword="null" /> when not
///     selecting one.
/// </param>
/// <remarks>
///     Member lookup supports a type-qualified name in the form <c>TypeName:MemberName</c>. When <paramref name="type" />
///     is <see langword="null" />, a qualified type name can instead be included as the leading portion of
///     <paramref name="memberName" />. Dotted names can traverse nested types, select a local function as
///     <c>OuterMethod.LocalFunction</c>, or select compiler-generated lambdas as <c>OuterMethod.*</c>. Only members declared
///     directly by the resolved type are considered. In <paramref name="parameterTypes" />, use <see cref="Ref{T}" />,
///     <see cref="In{T}" />, or <see cref="Out{T}" /> to match <see langword="ref" />, <see langword="in" />, or
///     <see langword="out" /> parameters.
/// </remarks>
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
    /// <summary>
    ///     Runs the patch after each access or call to the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    public InnerPostfixAttribute(Type type, string? memberName) : this(type, memberName, MemberType.Any)
    { }

    /// <summary>
    ///     Runs the patch after each call to the specified overload of the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public InnerPostfixAttribute(Type type, string? memberName, params Type[] parameterTypes) : this(type, memberName, MemberType.Any,
        parameterTypes) { }
}

/// <summary>
///     Marks a patch method to run after each occurrence of the specified constant in the selected outer methods.
///     Use <see cref="ReturnValueAttribute" /> on a patch parameter to inspect or replace the constant value.
/// </summary>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPostfixConstantAttribute : PatchTypeAttribute
{
    public readonly object value;

    /// <summary>
    ///     Runs the patch after each matching 32-bit integer constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerPostfixConstantAttribute(int value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching 64-bit integer constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerPostfixConstantAttribute(long value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching single-precision floating-point constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerPostfixConstantAttribute(float value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching double-precision floating-point constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerPostfixConstantAttribute(double value) : base(PatchType.InnerPostfix)
    {
        this.value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching string constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
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

/// <summary>
///     Marks the selected target members for Disharmony's experimental, optional IL optimization pass after their patches
///     are applied. The optimizer must be enabled separately.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class OptimizeAttribute : Attribute;

/// <summary>
///     Identifies a single member to which the attributed patch method applies. The member type, parameter types, and
///     generic type arguments can be supplied to distinguish overloads.
/// </summary>
/// <param name="type">
///     The type that declares the target member, or <see langword="null" /> to resolve it from
///     <paramref name="methodName" /> or use the type declared by a containing <see cref="HarmonyLib.HarmonyPatch" />
///     attribute.
/// </param>
/// <param name="methodName">
///     The name of the target member, or <see langword="null" /> when targeting a constructor.
/// </param>
/// <param name="memberType">The kind of member or accessor to target.</param>
/// <param name="parameterTypes">
///     The parameter types used to select an overload, or <see langword="null" /> to match without a parameter signature.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used when matching a generic method, or <see langword="null" /> when not selecting one.
/// </param>
/// <remarks>
///     Member lookup supports a type-qualified name in the form <c>TypeName:MemberName</c>. When <paramref name="type" />
///     is <see langword="null" />, a qualified type name can instead be included as the leading portion of
///     <paramref name="methodName" />. Dotted names can traverse nested types, select a local function as
///     <c>OuterMethod.LocalFunction</c>, or select compiler-generated lambdas as <c>OuterMethod.*</c>. Only members declared
///     directly by the resolved type are considered. In <paramref name="parameterTypes" />, use <see cref="Ref{T}" />,
///     <see cref="In{T}" />, or <see cref="Out{T}" /> to match <see langword="ref" />, <see langword="in" />, or
///     <see langword="out" /> parameters. Use <see cref="TargetsAttribute" /> when a name is expected to match more than one
///     member.
/// </remarks>
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

    /// <summary>
    ///     Applies the patch to a named member on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target member, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or from containing Harmony patch metadata.
    /// </param>
    /// <param name="methodName">The name of the target member.</param>
    public TargetAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to a specific overload on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target member, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or from containing Harmony patch metadata.
    /// </param>
    /// <param name="methodName">The name of the target member.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public TargetAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    /// <summary>
    ///     Applies the patch to a member whose declaring type is resolved from the member name or containing Harmony patch
    ///     metadata.
    /// </summary>
    /// <param name="methodName">
    ///     The name of the target member, or <see langword="null" /> when targeting a constructor.
    /// </param>
    /// <param name="memberType">The kind of member or accessor to target.</param>
    /// <param name="parameterTypes">
    ///     The parameter types used to select an overload, or <see langword="null" /> to match without a parameter signature.
    /// </param>
    /// <param name="genericTypes">
    ///     The generic type arguments used when matching a generic method, or <see langword="null" /> when not selecting one.
    /// </param>
    public TargetAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    /// <summary>
    ///     Applies the patch to a member whose declaring type is resolved from the member name or containing Harmony patch
    ///     metadata.
    /// </summary>
    /// <param name="methodName">The name of the target member.</param>
    public TargetAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to a specific overload whose declaring type is resolved from the member name or containing
    ///     Harmony patch metadata.
    /// </summary>
    /// <param name="methodName">The name of the target member.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public TargetAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

/// <summary>
///     Identifies every member matching the supplied criteria as a target of the attributed patch method. Use
///     <see cref="TargetAttribute" /> instead when the criteria must resolve to exactly one member.
/// </summary>
/// <param name="type">
///     The type that declares the target members, or <see langword="null" /> to resolve it from
///     <paramref name="methodName" /> or use the type declared by a containing <see cref="HarmonyLib.HarmonyPatch" />
///     attribute.
/// </param>
/// <param name="methodName">
///     The name shared by the target members, or <see langword="null" /> when targeting constructors.
/// </param>
/// <param name="memberType">The kind of members or accessors to target.</param>
/// <param name="parameterTypes">
///     The parameter types used to filter overloads, or <see langword="null" /> to match without a parameter signature.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used when matching generic methods, or <see langword="null" /> when not selecting them.
/// </param>
/// <remarks>
///     Member lookup supports a type-qualified name in the form <c>TypeName:MemberName</c>. When <paramref name="type" />
///     is <see langword="null" />, a qualified type name can instead be included as the leading portion of
///     <paramref name="methodName" />. Dotted names can traverse nested types, select a local function as
///     <c>OuterMethod.LocalFunction</c>, or select compiler-generated lambdas as <c>OuterMethod.*</c>. Only members declared
///     directly by the resolved type are considered. In <paramref name="parameterTypes" />, use <see cref="Ref{T}" />,
///     <see cref="In{T}" />, or <see cref="Out{T}" /> to match <see langword="ref" />, <see langword="in" />, or
///     <see langword="out" /> parameters.
/// </remarks>
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
    /// <summary>
    ///     Applies the patch to every member with the specified name on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target members, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or from containing Harmony patch metadata.
    /// </param>
    /// <param name="methodName">The name shared by the target members.</param>
    public TargetsAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to every matching overload on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target members, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or from containing Harmony patch metadata.
    /// </param>
    /// <param name="methodName">The name shared by the target members.</param>
    /// <param name="parameterTypes">The parameter types used to filter the overloads.</param>
    public TargetsAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    /// <summary>
    ///     Applies the patch to every matching member whose declaring type is resolved from the member name or containing
    ///     Harmony patch metadata.
    /// </summary>
    /// <param name="methodName">
    ///     The name shared by the target members, or <see langword="null" /> when targeting constructors.
    /// </param>
    /// <param name="memberType">The kind of members or accessors to target.</param>
    /// <param name="parameterTypes">
    ///     The parameter types used to filter overloads, or <see langword="null" /> to match without a parameter signature.
    /// </param>
    /// <param name="genericTypes">
    ///     The generic type arguments used when matching generic methods, or <see langword="null" /> when not selecting them.
    /// </param>
    public TargetsAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    /// <summary>
    ///     Applies the patch to every matching member whose declaring type is resolved from the member name or containing
    ///     Harmony patch metadata.
    /// </summary>
    /// <param name="methodName">The name shared by the target members.</param>
    public TargetsAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to every matching overload whose declaring type is resolved from the member name or containing
    ///     Harmony patch metadata.
    /// </summary>
    /// <param name="methodName">The name shared by the target members.</param>
    /// <param name="parameterTypes">The parameter types used to filter the overloads.</param>
    public TargetsAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

/// <summary>
///     Requests that the patch method's body be inlined into each selected target member instead of being invoked by a
///     method call.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class InlineAttribute : Attribute;

public abstract class ParameterBindingAttribute(Scope scope) : Attribute
{
    public readonly Scope scope = scope;
}

/// <summary>
///     Binds a patch parameter to a parameter of the outer method or inner method. The source parameter can be selected by
///     name or zero-based index, and <see cref="Scope" /> distinguishes the two methods for an inner patch.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ParameterAttribute : ParameterBindingAttribute
{
    public readonly int? index = null;
    public readonly string? name = null;

    /// <summary>
    ///     Binds to the source parameter having the same name as the attributed patch parameter.
    /// </summary>
    /// <param name="scope">
    ///     The method whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
    ///     method first and then the outer method.
    /// </param>
    public ParameterAttribute(Scope scope = Scope.Any) : base(scope) { }

    /// <summary>
    ///     Binds to a source parameter by name.
    /// </summary>
    /// <param name="name">
    ///     The source parameter name, or <see langword="null" /> to use the attributed patch parameter's name.
    /// </param>
    /// <param name="scope">
    ///     The method whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
    ///     method first and then the outer method.
    /// </param>
    public ParameterAttribute(string? name, Scope scope = Scope.Any) : base(scope)
    {
        this.name = name;
    }

    /// <summary>
    ///     Binds to a source parameter by position.
    /// </summary>
    /// <param name="index">The zero-based index of the source parameter, excluding the instance argument.</param>
    /// <param name="scope">
    ///     The method whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, uses the inner
    ///     method for an inner patch and the outer method otherwise.
    /// </param>
    public ParameterAttribute(int index, Scope scope = Scope.Any) : base(scope)
    {
        this.index = index;
    }
}

/// <summary>
///     Binds a patch parameter to the instance on which the outer method or inner method is invoked.
/// </summary>
/// <param name="scope">
///     The method whose instance is bound in an inner patch. The default, <see cref="Scope.Any" />, uses the inner method
///     for an inner patch and the outer method otherwise.
/// </param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class InstanceAttribute(Scope scope = Scope.Any) : ParameterBindingAttribute(scope);

/// <summary>
///     Binds a patch parameter to the outer method's return value for an ordinary patch, or the inner method's return value
///     for an inner patch. Pass the parameter by reference to replace that return value.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ReturnValueAttribute() : ParameterBindingAttribute(Scope.Any);

/// <summary>
///     Binds a patch parameter to temporary state for the current invocation of the outer method. Patch methods in the
///     same class can share state by using the same key and value type.
/// </summary>
/// <param name="key">
///     The key used to identify the shared state, or <see langword="null" /> to use the attributed patch parameter's name.
/// </param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class StateAttribute(string? key) : ParameterBindingAttribute(Scope.Outer)
{
    public readonly string? key = key;

    public StateAttribute() : this(null) { }
}

/// <summary>
///     Binds a patch parameter to an instance field associated with the outer method or inner method. The field can be
///     selected by name, and <see cref="Scope" /> distinguishes the two methods for an inner patch.
/// </summary>
/// <param name="name">
///     The field name, or <see langword="null" /> to use the attributed patch parameter's name.
/// </param>
/// <param name="scope">
///     The method whose instance is searched in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
///     method's instance first and then the outer method's instance.
/// </param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class FieldAttribute(string? name, Scope scope = Scope.Any) : ParameterBindingAttribute(scope)
{
    public readonly string? name = name;

    /// <summary>
    ///     Binds to the field having the same name as the attributed patch parameter.
    /// </summary>
    /// <param name="scope">
    ///     The method whose instance is searched in an inner patch. The default, <see cref="Scope.Any" />, searches the
    ///     inner method's instance first and then the outer method's instance.
    /// </param>
    public FieldAttribute(Scope scope = Scope.Any) : this(null, scope) { }
}

/// <summary>
///     Binds a patch parameter to a delegate that invokes the nearest base-class implementation of the outer instance
///     method, allowing the patch to call that implementation without virtual dispatch.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class BaseMethodAttribute() : ParameterBindingAttribute(Scope.Outer);
