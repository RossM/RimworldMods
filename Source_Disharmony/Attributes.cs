using JetBrains.Annotations;

namespace Disharmony;

/// <summary>
///     Represents a <see langword="ref" /> <typeparamref name="T" /> parameter in a target or inner-member signature.
/// </summary>
/// <typeparam name="T">The parameter's element type.</typeparam>
[PublicAPI]
public static class Ref<T>;

/// <summary>
///     Represents an <see langword="in" /> <typeparamref name="T" /> parameter in a target or inner-member signature.
/// </summary>
/// <typeparam name="T">The parameter's element type.</typeparam>
[PublicAPI]
public static class In<T>;

/// <summary>
///     Represents an <see langword="out" /> <typeparamref name="T" /> parameter in a target or inner-member signature.
/// </summary>
/// <typeparam name="T">The parameter's element type.</typeparam>
[PublicAPI]
public static class Out<T>;

/// <summary>
///     Specifies the kind of member or member access selected by a target or inner-patch attribute.
/// </summary>
[PublicAPI]
public enum MemberType
{
    /// <summary>
    ///     Matches methods, property getters, and field read accesses.
    /// </summary>
    Any,

    /// <summary>
    ///     Matches methods.
    /// </summary>
    Method,

    /// <summary>
    ///     Matches property getters and field read accesses.
    /// </summary>
    Getter,

    /// <summary>
    ///     Matches property setters and field write accesses.
    /// </summary>
    Setter,

    /// <summary>
    ///     Matches constructors.
    /// </summary>
    Constructor,
}

/// <summary>
///     Specifies whether an inner patch binds data from the inner member or its containing outer member.
/// </summary>
public enum Scope
{
    /// <summary>
    ///     Uses the default scope for the binding.
    /// </summary>
    /// <remarks>
    ///     Ordinary patches use the outer member. Inner patches use the inner member, except that name-based parameter and
    ///     field lookups fall back to the outer member when the inner member has no match.
    /// </remarks>
    Any,

    /// <summary>
    ///     Uses data from the inner member.
    /// </summary>
    Inner,

    /// <summary>
    ///     Uses data from the outer member.
    /// </summary>
    Outer,
}

/// <summary>
///     Lists the optional behaviors that can be enabled for a patch.
/// </summary>
/// <remarks>
///     For an attributed patch, pass the desired flags to <see cref="PatchOptionsAttribute" />. When configuring a patch
///     entirely in code, pass them to the <c>options</c> parameter of the appropriate <c>Register</c> or <c>Patch</c>
///     overload.
/// </remarks>
[Flags]
public enum PatchOptions
{
    /// <summary>
    ///     Enables no additional behavior.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     Runs the patch as part of each target instead of making a separate call to the patch method.
    /// </summary>
    Inline = 0x1,

    /// <summary>
    ///     Requests Disharmony's optional, experimental IL optimization pass.
    /// </summary>
    /// <remarks>
    ///     The optimization pass must be enabled separately.
    /// </remarks>
    Optimize = 0x2,

    /// <summary>
    ///     Requires that a patch always runs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On a prefix, this flag causes the prefix to always run before other prefixes. Prefixes with
    ///         this flag must return void and can't modify the function return value.
    ///     </para>
    ///     <para>
    ///         On a postfix, this flag causes the postfix to run even if the method or another patch throws
    ///         an exception. The method may use <c>__exception</c> or <see cref="ExceptionAttribute"/> to
    ///         inspect or change the exception.
    ///     </para>
    ///     <para>
    ///         A patch method with this flag should never throw an exception itself. If it does, other prefixes
    ///         and postfixes with this flag will be skipped.
    ///     </para>
    /// </remarks>
    AlwaysRun = 0x4,

    /// <summary>
    ///     Logs the modified IL and, when available, the generated Mono JIT assembly.
    /// </summary>
    /// <remarks>
    ///     Output is written to the Harmony debug log.
    /// </remarks>
    Debug = 0x8000,
}

/// <summary>
///     Marks a class as a patch container for assembly discovery and optionally supplies its default target type.
/// </summary>
/// <param name="type">
///     The default type that declares the outer target members, or <see langword="null" /> when target attributes identify
///     their declaring types individually.
/// </param>
/// <remarks>
///     <para>
///         <see cref="Patcher.PatchAll(Assembly)" /> and
///         <see cref="Patcher.PatchCategory" /> discover classes marked
///         with this attribute. Direct registration by type or method does not require it.
///     </para>
///     <para>
///         A type specified directly by <see cref="TargetAttribute" /> or <see cref="TargetsAttribute" /> takes
///         precedence.
///         Harmony's <see cref="HarmonyLib.HarmonyPatch" /> is also recognized for compatibility, but this attribute is
///         preferred for new patch classes. See <see cref="Patcher" /> for the complete authoring and targeting model.
///     </para>
/// </remarks>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class PatchAttribute(Type? type = null) : Attribute
{
    public Type? Type { get; } = type;
}

/// <summary>
///     Assigns a patch container to a named category for category-specific assembly discovery.
/// </summary>
/// <param name="category">The category name used to select the patch class.</param>
/// <remarks>
///     <para>
///         <see cref="Patcher.PatchCategory" /> compares this name with
///         the requested category. <see cref="Patcher.PatchAll(Assembly)" /> ignores
///         categories.
///     </para>
///     <para>
///         This attribute does not make the class discoverable by itself. New patch containers should also use
///         <see cref="PatchAttribute" />. Existing Harmony metadata can instead supply
///         <see cref="HarmonyLib.HarmonyPatch" /> as the patch marker, and
///         <see cref="HarmonyLib.HarmonyPatchCategory" /> is recognized as an alternative category declaration.
///     </para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class CategoryAttribute(string category) : Attribute
{
    public string Category { get; } = category;
}

public abstract class PatchTypeAttribute(PatchType patchType) : Attribute
{
    public PatchType PatchType { get; } = patchType;
}

/// <summary>
///     Marks a patch method to run before each selected outer member. A prefix that returns
///     <see langword="false" /> skips the outer member.
/// </summary>
/// <remarks>
///     Select outer members with <see cref="TargetAttribute" /> or <see cref="TargetsAttribute" />. See
///     <see cref="Patcher" /> for patch structure, targeting, and parameter binding.
/// </remarks>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PrefixAttribute() : PatchTypeAttribute(PatchType.Prefix);

/// <summary>
///     Marks a patch method to run after each selected outer member.
/// </summary>
/// <remarks>
///     Select outer members with <see cref="TargetAttribute" /> or <see cref="TargetsAttribute" />. Bind the outer return
///     value with <see cref="ReturnValueAttribute" /> or the conventional parameter name <c>__result</c>; pass it by
///     reference to replace it. See <see cref="Patcher" /> for the complete patch model.
/// </remarks>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PostfixAttribute() : PatchTypeAttribute(PatchType.Postfix);

public abstract class InnerAttributeBase : Attribute;

/// <summary>
///     Marks a patch method to run before each matching inner member access or call within the selected outer methods.
///     A prefix that returns <see langword="false" /> skips the matched access or call.
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
///     Select outer members with <see cref="TargetAttribute" /> or <see cref="TargetsAttribute" />. See
///     <see cref="Patcher" /> for member-name syntax, overload selection, and inner-versus-outer parameter binding.
/// </remarks>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerAttribute(
    Type type,
    string? memberName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null) : InnerAttributeBase
{
    public Type Type { get; } = type;
    public string? MemberName { get; } = memberName;
    public MemberType MemberType { get; } = memberType;
    public Type[]? ParameterTypes { get; } = parameterTypes;
    public Type[]? GenericTypes { get; } = genericTypes;

    /// <summary>
    ///     Runs the patch before each access or call to the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    public InnerAttribute(Type type, string? memberName) : this(type, memberName, MemberType.Any) { }

    /// <summary>
    ///     Runs the patch before each call to the specified overload of the named inner member.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the inner member, or <see langword="null" /> to resolve it from
    ///     <paramref name="memberName" />.
    /// </param>
    /// <param name="memberName">The name of the inner member to match.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public InnerAttribute(Type type, string? memberName, params Type[] parameterTypes) : this(type, memberName, MemberType.Any,
        parameterTypes) { }
}

/// <summary>
///     Marks a patch method to run after each occurrence of the specified constant in the selected outer methods.
/// </summary>
/// <remarks>
///     Bind the constant value with <see cref="ReturnValueAttribute" /> or the conventional parameter name
///     <c>__result</c>; pass it by reference to replace it. Select outer members with <see cref="TargetAttribute" /> or
///     <see cref="TargetsAttribute" />. See <see cref="Patcher" /> for the complete patch model.
/// </remarks>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerConstantAttribute : InnerAttributeBase
{
    public object Value { get; }

    /// <summary>
    ///     Runs the patch after each matching 32-bit integer constant in IL.
    /// </summary>
    /// <remarks>
    ///     IL provides only 32-bit and 64-bit integer constant forms, so constants of smaller integer types, including
    ///     <see langword="bool" />, are emitted as 32-bit values. <see langword="true" /> is represented as 1, and
    ///     <see langword="false" /> is represented as 0. Exercise caution when patching 0 or 1 because they can match
    ///     <see langword="bool" /> values introduced by the compiler.
    /// </remarks>
    /// <param name="value">The constant value to match.</param>
    public InnerConstantAttribute(int value)
    {
        Value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching 64-bit integer constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerConstantAttribute(long value)
    {
        Value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching single-precision floating-point constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerConstantAttribute(float value)
    {
        Value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching double-precision floating-point constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerConstantAttribute(double value)
    {
        Value = value;
    }

    /// <summary>
    ///     Runs the patch after each matching string constant.
    /// </summary>
    /// <param name="value">The constant value to match.</param>
    public InnerConstantAttribute(string value)
    {
        Value = value;
    }
}

/// <summary>
///     Enables optional behaviors for the attributed patch method or patch class.
/// </summary>
/// <param name="options">The behaviors to enable. Multiple <see cref="PatchOptions" /> flags can be combined.</param>
/// <remarks>
///     Apply this attribute to a patch class to provide options for every patch method declared by the class. An attribute
///     on an individual patch method replaces the class-level options for that method. The <c>Register</c> and
///     <c>Patch</c> overloads that accept a <see cref="PatchOptions" /> argument use that argument instead.
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PatchOptionsAttribute(PatchOptions options) : Attribute
{
    public PatchOptions Options { get; } = options;
}

/// <summary>
///     Sets the priority of a patch. The meaning of priority depends on the patch type. For <see cref="PrefixAttribute">prefix</see>
///     patches, higher priority patches run earlier, while for other patch types higher priority patches run later.
/// </summary>
/// <param name="priority"></param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}

[PublicAPI]
public static class PatchPriority
{
    public const int VeryLow = 0;
    public const int Low = 500;
    public const int Default = 1000;
    public const int High = 1500;
    public const int VeryHigh = 2000;
}

/// <summary>
///     Selects exactly one outer method, constructor, or property accessor for the attributed patch method or methods.
/// </summary>
/// <param name="type">
///     The type that declares the target, or <see langword="null" /> to use the default from a containing
///     <see cref="PatchAttribute" /> or Harmony patch attribute, or to resolve it from <paramref name="methodName" />.
/// </param>
/// <param name="methodName">
///     The name of the target member, or <see langword="null" /> when targeting a constructor.
/// </param>
/// <param name="memberType">The kind of member or accessor to target.</param>
/// <param name="parameterTypes">
///     The parameter types used to select an overload, or <see langword="null" /> to omit parameter filtering.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used to identify a constructed generic method, or <see langword="null" /> when generic
///     type arguments are not part of the selection.
/// </param>
/// <remarks>
///     <para>
///         When applied to a class, this target applies to every patch method declared by the class. Method-level targets
///         are added to class-level targets.
///     </para>
///     <para>
///         The selection must resolve to exactly one member. Use <see cref="TargetsAttribute" /> to patch every match.
///         Constructed generic methods can be identified during lookup but are not currently supported as outer targets.
///     </para>
///     <para>
///         See <see cref="Patcher" /> for declaring-type precedence, member-name syntax, and overload selection.
///     </para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TargetAttribute(
    Type? type,
    string? methodName = null,
    MemberType memberType = MemberType.Any,
    Type[]? parameterTypes = null,
    Type[]? genericTypes = null)
    : Attribute
{
    public Type? Type { get; } = type;
    public string? MethodName { get; } = methodName;
    public MemberType MemberType { get; } = memberType;
    public Type[]? ParameterTypes { get; } = parameterTypes;
    public Type[]? GenericTypes { get; } = genericTypes;

    /// <summary>
    ///     Applies the patch to a named member on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target member, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or containing patch metadata.
    /// </param>
    /// <param name="methodName">The name of the target member.</param>
    public TargetAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to a specific overload on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target member, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or containing patch metadata.
    /// </param>
    /// <param name="methodName">The name of the target member.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public TargetAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    /// <summary>
    ///     Applies the patch to a member whose declaring type is resolved from the member name or containing patch
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
    ///     The generic type arguments used to identify a constructed generic method, or <see langword="null" /> when
    ///     generic type arguments are not part of the selection.
    /// </param>
    public TargetAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    /// <summary>
    ///     Applies the patch to a member whose declaring type is resolved from the member name or containing patch
    ///     metadata.
    /// </summary>
    /// <param name="methodName">The name of the target member.</param>
    public TargetAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to a specific overload whose declaring type is resolved from the member name or containing
    ///     patch metadata.
    /// </summary>
    /// <param name="methodName">The name of the target member.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    public TargetAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

/// <summary>
///     Selects every outer method, constructor, or property accessor matching the supplied criteria for the attributed
///     patch method or methods.
/// </summary>
/// <param name="type">
///     The type that declares the targets, or <see langword="null" /> to use the default from a containing
///     <see cref="PatchAttribute" /> or Harmony patch attribute, or to resolve it from <paramref name="methodName" />.
/// </param>
/// <param name="methodName">
///     The name shared by the target members, or <see langword="null" /> when targeting constructors.
/// </param>
/// <param name="memberType">The kind of members or accessors to target.</param>
/// <param name="parameterTypes">
///     The parameter types used to filter overloads, or <see langword="null" /> to omit parameter filtering.
/// </param>
/// <param name="genericTypes">
///     The generic type arguments used to identify constructed generic methods, or <see langword="null" /> when generic
///     type arguments are not part of the selection.
/// </param>
/// <remarks>
///     <para>
///         When applied to a class, these targets apply to every patch method declared by the class. Method-level targets
///         are added to class-level targets.
///     </para>
///     <para>
///         Every match is patched; use <see cref="TargetAttribute" /> when the selection must resolve to exactly one
///         member. Constructed generic methods can be identified during lookup but are not currently supported as outer
///         targets.
///     </para>
///     <para>
///         See <see cref="Patcher" /> for declaring-type precedence, member-name syntax, and overload selection.
///     </para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
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
    ///     <paramref name="methodName" /> or containing patch metadata.
    /// </param>
    /// <param name="methodName">The name shared by the target members.</param>
    public TargetsAttribute(Type? type, string? methodName)
        : this(type, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to every matching overload on the specified type.
    /// </summary>
    /// <param name="type">
    ///     The type that declares the target members, or <see langword="null" /> to resolve it from
    ///     <paramref name="methodName" /> or containing patch metadata.
    /// </param>
    /// <param name="methodName">The name shared by the target members.</param>
    /// <param name="parameterTypes">The parameter types used to filter the overloads.</param>
    public TargetsAttribute(Type? type, string? methodName, params Type[] parameterTypes)
        : this(type, methodName, MemberType.Any, parameterTypes) { }

    /// <summary>
    ///     Applies the patch to every matching member whose declaring type is resolved from the member name or containing
    ///     patch metadata.
    /// </summary>
    /// <param name="methodName">
    ///     The name shared by the target members, or <see langword="null" /> when targeting constructors.
    /// </param>
    /// <param name="memberType">The kind of members or accessors to target.</param>
    /// <param name="parameterTypes">
    ///     The parameter types used to filter overloads, or <see langword="null" /> to match without a parameter signature.
    /// </param>
    /// <param name="genericTypes">
    ///     The generic type arguments used to identify constructed generic methods, or <see langword="null" /> when
    ///     generic type arguments are not part of the selection.
    /// </param>
    public TargetsAttribute(
        string? methodName = null,
        MemberType memberType = MemberType.Any,
        Type[]? parameterTypes = null,
        Type[]? genericTypes = null)
        : this(null, methodName, memberType, parameterTypes, genericTypes) { }

    /// <summary>
    ///     Applies the patch to every matching member whose declaring type is resolved from the member name or containing
    ///     patch metadata.
    /// </summary>
    /// <param name="methodName">The name shared by the target members.</param>
    public TargetsAttribute(string methodName)
        : this(null, methodName, MemberType.Any) { }

    /// <summary>
    ///     Applies the patch to every matching overload whose declaring type is resolved from the member name or containing
    ///     patch metadata.
    /// </summary>
    /// <param name="methodName">The name shared by the target members.</param>
    /// <param name="parameterTypes">The parameter types used to filter the overloads.</param>
    public TargetsAttribute(string methodName, params Type[] parameterTypes)
        : this(null, methodName, MemberType.Any, parameterTypes) { }
}

public abstract class ParameterBindingAttribute(Scope scope) : Attribute
{
    public Scope Scope { get; } = scope;
}

/// <summary>
///     Binds a patch parameter to a parameter of either the outer or inner member. The source parameter can be selected by
///     name or zero-based index, and <see cref="Scope" /> selects the member for an inner patch.
/// </summary>
/// <remarks>
///     Patch parameters bind by name without this attribute. Use it when the patch parameter has a different name, when
///     positional binding is more stable, or when an inner patch must select a specific scope. See
///     <see cref="Patcher" /> for implicit binding conventions.
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ParameterAttribute : ParameterBindingAttribute
{
    public int? Index { get; } = null;
    public string? Name { get; } = null;

    /// <summary>
    ///     Binds to the source parameter having the same name as the attributed patch parameter.
    /// </summary>
    /// <param name="scope">
    ///     The member whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
    ///     member first and then the outer member.
    /// </param>
    public ParameterAttribute(Scope scope = Scope.Any) : base(scope) { }

    /// <summary>
    ///     Binds to a source parameter by name.
    /// </summary>
    /// <param name="name">
    ///     The source parameter name, or <see langword="null" /> to use the attributed patch parameter's name.
    /// </param>
    /// <param name="scope">
    ///     The member whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
    ///     member first and then the outer member.
    /// </param>
    public ParameterAttribute(string? name, Scope scope = Scope.Any) : base(scope)
    {
        Name = name;
    }

    /// <summary>
    ///     Binds to a source parameter by position.
    /// </summary>
    /// <param name="index">The zero-based index of the source parameter, excluding the instance argument.</param>
    /// <param name="scope">
    ///     The member whose parameter is bound in an inner patch. The default, <see cref="Scope.Any" />, uses the inner
    ///     member for an inner patch and the outer member otherwise.
    /// </param>
    public ParameterAttribute(int index, Scope scope = Scope.Any) : base(scope)
    {
        Index = index;
    }
}

/// <summary>
///     Binds a patch parameter to the instance on which either the outer or inner member is invoked.
/// </summary>
/// <param name="scope">
///     The member whose instance is bound in an inner patch. The default, <see cref="Scope.Any" />, uses the inner member
///     for an inner patch and the outer member otherwise.
/// </param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class InstanceAttribute(Scope scope = Scope.Any) : ParameterBindingAttribute(scope);

/// <summary>
///     Binds a patch parameter to the outer member's return value for an ordinary patch or the inner member's return value
///     for an inner patch. Pass the parameter by reference to replace the bound return value.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ReturnValueAttribute() : ParameterBindingAttribute(Scope.Any);

/// <summary>
///     Binds a patch parameter to temporary state for the current invocation of the outer member.
/// </summary>
/// <param name="key">
///     The key used to identify the shared state, or <see langword="null" /> to use the attributed patch parameter's name.
/// </param>
/// <remarks>
///     <para>
///         State is not supported when an inner patch targets an iterator state-machine method.
///     </para>
///     <para>
///         Patches applied with <see cref="Patcher.PatchAll(Assembly)"/>, <see cref="Patcher.PatchAll(Type)"/>, or
///         <see cref="Patcher.Patch(MethodInfo)"/> will share state with other patch methods in the same containing type
///         that use the same key.
///         Patches applied using <see cref="PatchConfig"/> will share state with other methods applied during the same
///         <c>Patcher.Patch</c> call that use the same key.
///     </para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class StateAttribute(string? key) : ParameterBindingAttribute(Scope.Outer)
{
    public string? Key { get; } = key;

    /// <summary>
    ///     Binds temporary state using the attributed patch parameter's name as the key.
    /// </summary>
    public StateAttribute() : this(null) { }
}

/// <summary>
///     Binds a patch parameter to an instance field associated with either the outer or inner member. The field can be
///     selected by name, and <see cref="Scope" /> selects the member for an inner patch.
/// </summary>
/// <param name="name">
///     The field name, or <see langword="null" /> to use the attributed patch parameter's name.
/// </param>
/// <param name="scope">
///     The member whose instance is searched in an inner patch. The default, <see cref="Scope.Any" />, searches the inner
///     member's instance first and then the outer member's instance.
/// </param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class FieldAttribute(string? name, Scope scope = Scope.Any) : ParameterBindingAttribute(scope)
{
    public string? Name { get; } = name;

    /// <summary>
    ///     Binds to the field having the same name as the attributed patch parameter.
    /// </summary>
    /// <param name="scope">
    ///     The member whose instance is searched in an inner patch. The default, <see cref="Scope.Any" />, searches the
    ///     inner member's instance first and then the outer member's instance.
    /// </param>
    public FieldAttribute(Scope scope = Scope.Any) : this(null, scope) { }
}

/// <summary>
///     Binds a patch parameter to a delegate that invokes the nearest base-class implementation of the outer instance
///     method, allowing the patch to call that implementation directly as if using <see langword="base" />.
/// </summary>
/// <remarks>
///     The patch parameter must be a delegate whose parameters and return type match the outer method. Static methods do
///     not have a base-method binding.
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class BaseMethodAttribute() : ParameterBindingAttribute(Scope.Outer);

/// <summary>
///     Binds a patch parameter to a delegate that invokes a given method on the class of the inner or outer instance.
/// </summary>
/// <remarks>
///     This can be used to access otherwise-inaccessible methods on the classes of the instance.
/// </remarks>
/// <param name="name"></param>
/// <param name="scope"></param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class MethodAttribute(string? name, Scope scope = Scope.Any) : ParameterBindingAttribute(scope)
{
    public string? Name { get; } = name;

    public MethodAttribute(Scope scope = Scope.Any) : this(null, scope) { }
}

/// <summary>
///     Binds a patch parameter to the exception thrown by the method.
/// </summary>
/// <remarks>
///     <para>
///         If no exception is thrown, the value is <see langword="null"/>. If the parameter is <see langword="ref"/>
///         the method can replace the exception, or set it to <see langword="null"/> to suppress the exception.
///     </para>
///     <para>
///         This is only valid for <see cref="PostfixAttribute">postfixes</see> with the <see cref="PatchOptions.AlwaysRun"/>
///         option set.
///     </para>
///     <para>
///         A parameter named <c>__exception</c> will be bound to the exception automatically.
///     </para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public class ExceptionAttribute() : ParameterBindingAttribute(Scope.Any);