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

    /// <summary>
    ///     Runs before a matching operation within an outer member.
    /// </summary>
    InnerPrefix,

    /// <summary>
    ///     Runs after a matching operation within an outer member.
    /// </summary>
    InnerPostfix,
}

/// <summary>
///     Provides entry points for discovering, registering, applying, and removing Disharmony patches.
/// </summary>
/// <remarks>
///     <para>
///         A patch definition consists of a static patch method with one patch-kind attribute and at least one target.
///         <see cref="PrefixAttribute" /> and <see cref="PostfixAttribute" /> run around the selected target member, called
///         the outer member. <see cref="InnerPrefixAttribute" />, <see cref="InnerPostfixAttribute" />, and
///         <see cref="InnerPostfixConstantAttribute" /> instead run around matching calls, member accesses, or constants
///         within the outer member; the matched operation is called the inner member.
///     </para>
///     <para>
///         For assembly discovery, place patch methods in a class marked with <see cref="PatchAttribute" />.
///         <see cref="PatchAttribute.type" /> can provide a default declaring type for the class's targets, and
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
public static partial class Autopatcher
{
    // Lock order: applyLock, PatchRegistry.SyncRoot, Harmony's lock.
    private static readonly object applyLock = new();
    private static readonly PatchRegistry registry = PatchRegistry.Instance;
    private static readonly Patcher patcher = Patcher.Instance;

    /// <summary>
    ///     Discovers and registers every patch class in an assembly, then applies all pending patch changes.
    /// </summary>
    /// <param name="assembly">The assembly to scan for classes marked with <see cref="PatchAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatch" />.</param>
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
    /// <param name="assembly">The assembly to scan for classes marked with <see cref="PatchAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatch" />.</param>
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
    ///     Applies all pending patch changes using lazy trampolines.
    /// </summary>
    /// <remarks>
    ///     Each affected target is redirected immediately, but its final patched body is generated when the target is next
    ///     invoked. Use <see cref="ForceApply" /> when patch generation must complete before any target is called.
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
        patcher.ResolveAllTrampolines();
    }

    /// <summary>
    ///     Removes every registered Disharmony patch declared in an assembly and reapplies the affected targets.
    /// </summary>
    /// <param name="assembly">The assembly that declares the patch methods to remove.</param>
    /// <remarks>
    ///     This removes patches by the assembly containing each patch method, regardless of how those methods were
    ///     registered. It does not remove patches installed independently through Harmony.
    /// </remarks>
    public static void UnpatchAll(Assembly assembly)
    {
        registry.UnpatchAll(assembly);
        registry.ApplyImpl(useTrampolines: true);
    }
}
