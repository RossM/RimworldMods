using JetBrains.Annotations;

namespace Disharmony;

/// <summary>
///     Identifies where a patch runs relative to an outer member or an operation within it.
/// </summary>
public enum PatchType
{
    /// <summary>
    ///     Runs before an outer member.
    /// </summary>
    Prefix,

    /// <summary>
    ///     Runs after an outer member.
    /// </summary>
    Postfix,
}

/// <summary>
///     Provides entry points for discovering, registering, applying, and removing Disharmony patches.
/// </summary>
/// <remarks>
///     <para>
///         A patch definition consists of a static patch method with either <see cref="PrefixAttribute" /> or
///         <see cref="PostfixAttribute" /> and at least one target. By default, the patch runs before or after the
///         selected target member, called the outer member. Add <see cref="InnerAttribute" /> to run before or after
///         matching calls or member accesses within the outer member, or <see cref="InnerConstantAttribute" /> to run
///         before or after matching constants. The matched operation is called the inner member.
///     </para>
///     <para>
///         Use <see cref="PatchOptionsAttribute" /> to enable optional behavior for an attributed patch. An attribute on a
///         patch class provides options for all of its patch methods; an attribute on an individual method replaces those
///         options for that method.
///     </para>
///     <para>
///         For assembly discovery, place patch methods in a class marked with <see cref="PatchAttribute" />.
///         <see cref="PatchAttribute.Type" /> can provide a default declaring type for the class's targets, and
///         <see cref="CategoryAttribute" /> can restrict category-specific discovery. Harmony's
///         <see cref="HarmonyLib.HarmonyPatch" /> and <see cref="HarmonyLib.HarmonyPatchCategory" /> attributes are also
///         recognized for compatibility, but the Disharmony attributes are preferred for new patches. Direct
///         registration by <see cref="Type" /> or <see cref="MethodInfo" /> does not require a class-level patch marker.
///     </para>
///     <para>
///         Use <see cref="TargetAttribute" /> when the selection must resolve to one outer member and
///         <see cref="TargetsAttribute" /> when every match should be patched. Target attributes on a class apply to every
///         patch method declared by that class; method-level targets are added to any class-level targets. Repeating
///         <see cref="TargetAttribute" /> applies the same patch method to multiple outer members.
///     </para>
///     <para>
///         When the patch method uses attributes to describe how it runs but the outer targets are chosen in code, pass
///         those targets to the method-level overloads of
///         <see cref="Register(MethodInfo, IEnumerable{MethodBase})" /> and
///         <see cref="Patch(MethodInfo, IEnumerable{MethodBase})" /> instead of adding target attributes. To configure the
///         entire patch in code, use an overload that also accepts a <see cref="PatchType" />. Parameter-binding
///         attributes on the patch method work with either approach.
///     </para>
///     <para>
///         A target's declaring type is resolved from the target attribute first, then from the containing
///         <see cref="PatchAttribute" /> or Harmony patch metadata, and finally from the member name. When the name must
///         supply the type, write the type and member as a dotted name, such as <c>Namespace.Type.Member</c>. The
///         Harmony-style spelling <c>Namespace.Type:Member</c> is also accepted. Additional dotted segments can traverse
///         nested types, select a local function as <c>OuterMethod.LocalFunction</c>, or select compiler-generated lambdas
///         as <c>OuterMethod.*</c>. Member lookup considers only members declared directly by the resolved type.
///     </para>
///     <para>
///         Use <see cref="MemberType" /> to distinguish methods, constructors, property accessors, and, for inner patches,
///         field accesses. Supply parameter types to select an overload; use <see cref="Ref{T}" />,
///         <see cref="In{T}" />, and <see cref="Out{T}" /> for by-reference parameter types. Generic type arguments can
///         identify a constructed generic inner member, but constructed generic methods are not currently supported as
///         outer targets.
///     </para>
///     <para>
///         Patch method parameters bind to source parameters with the same name by default. Pass a value by reference to
///         replace it where the patch kind permits. The conventional names <c>__instance</c>, <c>__result</c>,
///         <c>__state</c>, <c>__base</c>, and <c>___fieldName</c> bind the target instance, return value, shared state,
///         nearest base implementation, and an instance field, respectively. In an inner patch, <c>__instance</c> and
///         <c>__result</c> refer to the inner member, <c>__caller</c> binds the outer instance, and ordinary parameter and
///         field lookup searches the inner member before the outer member. State and base-method bindings remain
///         associated with the outer member. The explicit
///         <see cref="ParameterAttribute" />, <see cref="InstanceAttribute" />, <see cref="ReturnValueAttribute" />,
///         <see cref="StateAttribute" />, <see cref="FieldAttribute" />, and <see cref="BaseMethodAttribute" /> attributes
///         provide the same bindings without relying on parameter-name conventions.
///     </para>
///     <para>
///         Registration records patch definitions but does not change target behavior. The <c>Patch</c> methods combine
///         registration with <see cref="Apply" />. Applying processes all pending changes in the process-wide registry,
///         not only the definitions registered by the immediately preceding call. <see cref="Apply" /> installs lazy
///         trampolines that finish patch generation when each target is next invoked; <see cref="ForceApply" /> generates
///         and installs all pending patches immediately.
///     </para>
///     <example>
///         A patch class can provide a default target type and then select members by name:
///         <code>
/// [Patch(typeof(Widget))]
/// public static class WidgetPatches
/// {
///     [Prefix]
///     [Target(nameof(Widget.Update))]
///     public static void UpdatePrefix(Widget __instance, ref int amount)
///     {
///         amount = Math.Max(amount, 0);
///     }
/// 
///     [Postfix]
///     [Target(nameof(Widget.GetValue))]
///     public static void GetValuePostfix(ref int __result)
///     {
///         __result *= 2;
///     }
/// }
/// 
/// Autopatcher.PatchAll(typeof(WidgetPatches).Assembly);
///         </code>
///     </example>
/// </remarks>
[PublicAPI]
public static class Patcher
{
    // Lock order: applyLock, PatchRegistry.SyncRoot, Harmony's lock.
    private static readonly object applyLock = new();
    private static readonly PatchRegistry registry = PatchRegistry.Instance;
    internal static readonly HarmonyInterface harmonyInterface = HarmonyInterface.Instance;

    public static event Action<Exception>? RuntimeExceptionHandler;

    internal static void ReportException(Exception exception)
    {
        if (RuntimeExceptionHandler != null)
            RuntimeExceptionHandler(exception);
        else
            FileLog.Log($"!!! Unhandled exception: {exception}");
    }

    /// <summary>
    ///     Discovers and registers every patch class in an assembly, then applies all pending patch changes.
    /// </summary>
    /// <param name="assembly">
    ///     The assembly to scan for classes marked with <see cref="PatchAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatch" />.
    /// </param>
    /// <remarks>
    ///     Categories are ignored. Use <see cref="PatchCategory" /> to select a single category.
    /// </remarks>
    public static void PatchAll(Assembly assembly)
    {
        RegisterAll(assembly);
        Apply();
    }

    /// <summary>
    ///     Discovers and registers every patch class in an assembly without applying the pending changes.
    /// </summary>
    /// <param name="assembly">
    ///     The assembly to scan for classes marked with <see cref="PatchAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatch" />.
    /// </param>
    /// <remarks>
    ///     Categories are ignored. Call <see cref="Apply" /> or <see cref="ForceApply" /> after completing registration.
    /// </remarks>
    public static void RegisterAll(Assembly assembly)
    {
        registry.ProcessAssembly(assembly);
    }

    /// <summary>
    ///     Discovers and registers patch classes in one category, then applies all pending patch changes.
    /// </summary>
    /// <param name="assembly">The assembly to scan for patch classes.</param>
    /// <param name="category">
    ///     The category to select, or <see langword="null" /> to select classes without a category.
    /// </param>
    /// <remarks>
    ///     Categories are supplied by <see cref="CategoryAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatchCategory" />. Classes must also have a recognized patch marker.
    /// </remarks>
    public static void PatchCategory(Assembly assembly, string? category)
    {
        RegisterCategory(assembly, category);
        Apply();
    }

    /// <summary>
    ///     Discovers and registers patch classes in one category without applying the pending changes.
    /// </summary>
    /// <param name="assembly">The assembly to scan for patch classes.</param>
    /// <param name="category">
    ///     The category to select, or <see langword="null" /> to select classes without a category.
    /// </param>
    /// <remarks>
    ///     Categories are supplied by <see cref="CategoryAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatchCategory" />. Classes must also have a recognized patch marker. Call
    ///     <see cref="Apply" /> or <see cref="ForceApply" /> after completing registration.
    /// </remarks>
    public static void RegisterCategory(Assembly assembly, string? category)
    {
        registry.ProcessAssembly(assembly, category);
    }

    /// <summary>
    ///     Registers every patch method declared by a type without applying the pending changes.
    /// </summary>
    /// <param name="type">The type that declares the patch methods to register.</param>
    /// <remarks>
    ///     The type does not need a <see cref="PatchAttribute" /> when registered directly. Inherited methods are not
    ///     processed. Call <see cref="Apply" /> or <see cref="ForceApply" /> after completing registration.
    /// </remarks>
    public static void Register(Type type)
    {
        registry.ProcessType(type.GetTypeInfo());
    }

    /// <summary>
    ///     Registers every patch method declared by a type, then applies all pending patch changes.
    /// </summary>
    /// <param name="type">The type that declares the patch methods to register.</param>
    /// <remarks>
    ///     The type does not need a <see cref="PatchAttribute" /> when patched directly. Inherited methods are not
    ///     processed.
    /// </remarks>
    public static void Patch(Type type)
    {
        Register(type);
        Apply();
    }

    /// <summary>
    ///     Registers one patch method without applying the pending changes.
    /// </summary>
    /// <param name="method">The patch method to register.</param>
    /// <remarks>
    ///     Attributes on the method and its declaring type are both considered. The declaring type does not need a
    ///     <see cref="PatchAttribute" /> when the method is registered directly. Call <see cref="Apply" /> or
    ///     <see cref="ForceApply" /> after completing registration.
    /// </remarks>
    public static void Register(MethodInfo method)
    {
        registry.ProcessMethod(method);
    }

    /// <summary>
    ///     Registers an attributed patch method for the supplied outer targets without making the patch take effect.
    /// </summary>
    /// <param name="method">The static method that implements the patch.</param>
    /// <param name="targets">The methods and constructors whose behavior should be patched.</param>
    /// <remarks>
    ///     Use this overload when <see cref="PrefixAttribute" /> or <see cref="PostfixAttribute" />, optionally combined
    ///     with <see cref="InnerAttribute" /> or <see cref="InnerConstantAttribute" />, describes how the patch runs, but
    ///     the targets are chosen in code. <see cref="TargetAttribute" /> and <see cref="TargetsAttribute" /> are not
    ///     needed and are ignored. <see cref="PatchOptionsAttribute" /> and parameter-binding attributes still apply.
    ///     Call <see cref="Apply" /> or <see cref="ForceApply" /> when all patches have been registered.
    /// </remarks>
    public static void Register(MethodInfo method, params IEnumerable<MethodBase> targets)
    {
        registry.ProcessMethod(method, targets);
    }

    /// <summary>
    ///     Registers a patch described in code without making it take effect.
    /// </summary>
    /// <param name="method">The static method that implements the patch.</param>
    /// <param name="patchType">
    ///     Whether the patch runs before or after the selected operation.
    /// </param>
    /// <param name="innerTarget">
    ///     The method call, constructor call, or field access to match within each outer target, or
    ///     <see langword="null" /> to patch the outer targets themselves.
    /// </param>
    /// <param name="innerMemberType">
    ///     For an inner field, use <see cref="MemberType.Setter" /> to match writes; any other value matches reads. This has
    ///     no effect on inner methods or constructors, or on non-inner patches.
    /// </param>
    /// <param name="options">Additional behaviors, such as inlining the patch or producing debug output.</param>
    /// <param name="targets">The methods and constructors whose behavior should be patched.</param>
    /// <remarks>
    ///     Use this overload when the patch timing, inner operation, targets, and options are chosen in code. The patch
    ///     method does not need <see cref="PrefixAttribute" />, <see cref="PostfixAttribute" />,
    ///     <see cref="InnerAttribute" />, <see cref="InnerConstantAttribute" />, target, or
    ///     <see cref="PatchOptionsAttribute" /> attributes; if present, they are ignored. Attributes that bind patch
    ///     parameters remain effective. Call <see cref="Apply" /> or <see cref="ForceApply" /> when all patches have been
    ///     registered.
    /// </remarks>
    public static void Register(
        MethodInfo method,
        PatchType patchType,
        MemberInfo? innerTarget = null,
        MemberType innerMemberType = MemberType.Any,
        PatchOptions options = PatchOptions.Default,
        params IEnumerable<MethodBase> targets)
    {
        registry.ProcessMethod(method, patchType, innerTarget, innerMemberType, options, targets, method.DeclaringType!.FullName);
    }

    /// <summary>
    ///     Registers one patch method, then applies all pending patch changes.
    /// </summary>
    /// <param name="method">The patch method to register.</param>
    /// <remarks>
    ///     Attributes on the method and its declaring type are both considered. The declaring type does not need a
    ///     <see cref="PatchAttribute" /> when the method is patched directly.
    /// </remarks>
    public static void Patch(MethodInfo method)
    {
        Register(method);
        Apply();
    }

    /// <summary>
    ///     Patches the supplied outer targets using an attributed patch method.
    /// </summary>
    /// <param name="method">The static method that implements the patch.</param>
    /// <param name="targets">The methods and constructors whose behavior should be patched.</param>
    /// <remarks>
    ///     Use this overload when <see cref="PrefixAttribute" /> or <see cref="PostfixAttribute" />, optionally combined
    ///     with <see cref="InnerAttribute" /> or <see cref="InnerConstantAttribute" />, describes how the patch runs, but
    ///     the targets are chosen in code. <see cref="TargetAttribute" /> and <see cref="TargetsAttribute" /> are not
    ///     needed and are ignored. <see cref="PatchOptionsAttribute" /> and parameter-binding attributes still apply.
    ///     This call also makes all patches registered so far take effect. Use
    ///     <see cref="Register(MethodInfo, IEnumerable{MethodBase})" /> when more patches will be registered before
    ///     applying them together.
    /// </remarks>
    public static void Patch(MethodInfo method, params IEnumerable<MethodBase> targets)
    {
        Register(method, targets);
        Apply();
    }

    /// <summary>
    ///     Applies a patch whose behavior and targets are described in code.
    /// </summary>
    /// <param name="method">The static method that implements the patch.</param>
    /// <param name="patchType">
    ///     Whether the patch runs before or after the selected operation.
    /// </param>
    /// <param name="innerTarget">
    ///     The method call, constructor call, or field access to match within each outer target, or
    ///     <see langword="null" /> to patch the outer targets themselves.
    /// </param>
    /// <param name="innerMemberType">
    ///     For an inner field, use <see cref="MemberType.Setter" /> to match writes; any other value matches reads. This has
    ///     no effect on inner methods or constructors, or on non-inner patches.
    /// </param>
    /// <param name="options">Additional behaviors, such as inlining the patch or producing debug output.</param>
    /// <param name="targets">The methods and constructors whose behavior should be patched.</param>
    /// <remarks>
    ///     Use this overload when the patch timing, inner operation, targets, and options are chosen in code. The patch
    ///     method does not need <see cref="PrefixAttribute" />, <see cref="PostfixAttribute" />,
    ///     <see cref="InnerAttribute" />, <see cref="InnerConstantAttribute" />, target, or
    ///     <see cref="PatchOptionsAttribute" /> attributes; if present, they are ignored. Attributes that bind patch
    ///     parameters remain effective. This call also makes all patches registered so far take effect. Use
    ///     <see cref="Register(MethodInfo, PatchType, MemberInfo, MemberType, PatchOptions, IEnumerable{MethodBase})" />
    ///     when more patches will be registered before applying them together.
    /// </remarks>
    public static void Patch(
        MethodInfo method,
        PatchType patchType,
        MemberInfo? innerTarget = null,
        MemberType innerMemberType = MemberType.Any,
        PatchOptions options = PatchOptions.Default,
        params IEnumerable<MethodBase> targets)
    {
        Register(method, patchType, innerTarget, innerMemberType, options, targets);
        Apply();
    }

    public static PatchHandle Patch(PatchConfig patch)
    {
        return Patch([patch]);
    }

    public static PatchHandle Patch(PatchConfig patch, params IEnumerable<MethodBase> methods)
    {
        return Patch(methods.Select(patch.Of));
    }

    public static PatchHandle Patch(MethodBase method, params IEnumerable<PatchConfig> patches)
    {
        return Patch(patches.Select(patch => patch.Of(method)));
    }

    public static PatchHandle Patch(params IEnumerable<PatchConfig> patches)
    {
        throw new NotImplementedException();
    }

    public static void Unpatch(PatchHandle handle)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Activates all pending patch changes while deferring their expensive preparation until needed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Patches are effective when this method returns; calling <see cref="ForceApply" /> afterward is not required
    ///         for correctness. Most preparation is postponed until each affected method is first called, which keeps
    ///         initialization fast but can make that first call take longer.
    ///     </para>
    ///     <para>
    ///         Deferred preparation can also combine patches registered by multiple mods before the target is used. Call
    ///         <see cref="ForceApply" /> when the application would rather complete the deferred work during a chosen idle
    ///         period than during first use. Its remarks describe the just-in-time behavior and scheduling considerations
    ///         in more detail.
    ///     </para>
    /// </remarks>
    public static void Apply()
    {
        registry.ApplyImpl(useTrampolines: true);
    }

    /// <summary>
    ///     Applies all pending patch changes immediately and resolves any lazy trampolines installed by earlier calls.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Apply" /> is designed to return quickly during mod initialization. It makes each patch active
    ///         through a lightweight placeholder called a trampoline and postpones the more expensive work of producing
    ///         the completed patch. Disharmony finishes that work automatically when the target method is first called.
    ///         This is normally transparent, although the first call can take longer than later calls.
    ///     </para>
    ///     <para>
    ///         Call <see cref="ForceApply" /> to complete all currently deferred patching at a time chosen by the
    ///         application. For example, a mod can run it on a worker thread after initialization while the user is at a
    ///         menu, trading background work during an idle period for predictable performance when gameplay begins.
    ///         Disharmony does not choose that time or start a background thread itself.
    ///     </para>
    ///     <para>
    ///         Deferral also allows patches from different mods to accumulate. If several mods target the same method
    ///         before that method is first used, Disharmony can prepare it once with the complete set of patches instead of
    ///         preparing it again after each mod. For the greatest benefit, call <see cref="ForceApply" /> after other mods
    ///         have had an opportunity to register their patches. The method returns when all patching known at that time
    ///         is complete; patches registered later may create new deferred work.
    ///     </para>
    /// </remarks>
    public static void ForceApply()
    {
        registry.ApplyImpl(useTrampolines: false);
        harmonyInterface.ResolveAllTrampolines();
    }

    /// <summary>
    ///     Removes every registered Disharmony patch declared in an assembly and reapplies the affected targets.
    /// </summary>
    /// <remarks>
    ///     This removes patches by the assembly containing each patch method, regardless of how those methods were
    ///     registered. It does not remove patches installed independently through Harmony.
    /// </remarks>
    internal static void UnpatchAll()
    {
        registry.UnpatchAll();
        registry.ApplyImpl(useTrampolines: true);
    }
}
