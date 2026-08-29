using JetBrains.Annotations;

namespace Disharmony;

/// <summary>
///     Specifies whether a patch runs before or after its target operation.
/// </summary>
public enum PatchType
{
    /// <summary>
    ///     Runs before the target operation.
    /// </summary>
    Prefix,

    /// <summary>
    ///     Runs after the target operation.
    /// </summary>
    Postfix,
}

/// <summary>
///     Applies and removes Disharmony patches.
/// </summary>
/// <remarks>
///     <para>
///         A prefix runs before a target method or constructor, and a postfix runs after it. An inner patch runs around
///         matching calls, member accesses, or constants inside the target instead.
///     </para>
///     <para>
///         Patches can be defined in either of two ways:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Attribute-defined patches use <see cref="PrefixAttribute" /> or <see cref="PostfixAttribute" /> with
///                 <see cref="TargetAttribute" /> or <see cref="TargetsAttribute" />. Apply one with
///                 <see cref="Patch(MethodInfo)" />, all patches declared by a type with <see cref="PatchAll(Type)" />, or
///                 marked patch classes from an assembly with <see cref="PatchAll(Assembly)" /> or
///                 <see cref="PatchCategory" />.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Programmatic patches use <see cref="Disharmony.Patch" /> to build a <see cref="PatchConfig" />. Choose
///                 <c>Prefix</c> or <c>Postfix</c>, add the patch method with <c>With</c>, add the target with <c>Of</c>,
///                 then pass the result to a <c>Patch</c> overload. Patch-definition attributes are ignored in this form,
///                 but parameter-binding attributes still apply.
///             </description>
///         </item>
///     </list>
///     <para>
///         Patch methods must be static. A prefix can return <see langword="false" /> to skip the target operation; any
///         postfixes still run. Patch parameters bind to target values by name or through the parameter-binding
///         attributes. Passing a bound value by reference can replace it where supported.
///     </para>
///     <para>
///         Every call that applies patches returns a <see cref="PatchHandle" />. Keep the handle if the patches may need to
///         be removed later with <see cref="Unpatch" />. A handle returned for several configurations removes them
///         together, and those configurations can share <see cref="StateAttribute">state</see>. Patches affect every
///         caller in the current process.
///     </para>
///     <para>
///         Patches take effect before a patching call returns, but Disharmony may postpone some preparation until a
///         patched method is first called. Use <see cref="ForceApply" /> when that work should happen at a predictable
///         time instead.
///     </para>
/// </remarks>
[PublicAPI]
public static class Patcher
{
    // Lock order: applyLock, PatchRegistry.SyncRoot, Harmony's lock.
    private static readonly object applyLock = new();
    private static readonly PatchRegistry registry = PatchRegistry.Instance;
    internal static readonly HarmonyInterface harmonyInterface = HarmonyInterface.Instance;

    /// <summary>
    ///     Notifies subscribers when Disharmony encounters a recoverable patching error.
    /// </summary>
    /// <remarks>
    ///     When the event has no subscribers, the exception is written to Harmony's file log. Errors that Disharmony
    ///     cannot recover from are thrown to the caller instead.
    /// </remarks>
    public static event Action<Exception>? RuntimeExceptionHandler;

    /// <summary>
    ///     Reports a patching exception to the configured runtime handler or Harmony's file log.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    internal static void ReportException(Exception exception)
    {
        if (RuntimeExceptionHandler != null)
            RuntimeExceptionHandler(exception);
        else
            FileLog.Log($"!!! Unhandled exception: {exception}");
    }

    /// <summary>
    ///     Applies all attributed patches in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the patches.</param>
    /// <remarks>
    ///     Patch classes must be marked with <see cref="PatchAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatch" />. All categories are included; use <see cref="PatchCategory" /> to apply
    ///     only one.
    /// </remarks>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle PatchAll(Assembly assembly)
    {
        var handle = RegisterAll(assembly);
        Apply();
        return handle;
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
    /// <returns>A handle that owns every patch registered by this call.</returns>
    internal static PatchHandle RegisterAll(Assembly assembly)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessAssembly(assembly, handle.id);
        return handle;
    }

    /// <summary>
    ///     Applies the attributed patches in one category of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the patches.</param>
    /// <param name="category">
    ///     The category to select, or <see langword="null" /> to select classes without a category.
    /// </param>
    /// <remarks>
    ///     Categories are supplied by <see cref="CategoryAttribute" /> or
    ///     <see cref="HarmonyLib.HarmonyPatchCategory" />. Classes must also have a recognized patch marker.
    /// </remarks>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle PatchCategory(Assembly assembly, string? category)
    {
        var handle = RegisterCategory(assembly, category);
        Apply();
        return handle;
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
    /// <returns>A handle that owns every patch registered by this call.</returns>
    internal static PatchHandle RegisterCategory(Assembly assembly, string? category)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessAssembly(assembly, category, handle.id);
        return handle;
    }

    /// <summary>
    ///     Registers every patch method declared by a type without applying the pending changes.
    /// </summary>
    /// <param name="type">The type that declares the patch methods to register.</param>
    /// <remarks>
    ///     The type does not need a <see cref="PatchAttribute" /> when registered directly. Inherited methods are not
    ///     processed. Call <see cref="Apply" /> or <see cref="ForceApply" /> after completing registration.
    /// </remarks>
    /// <returns>A handle that owns every patch registered by this call.</returns>
    internal static PatchHandle RegisterAll(Type type)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessType(type.GetTypeInfo(), handle.id);
        return handle;
    }

    /// <summary>
    ///     Applies all attributed patch methods declared by a type.
    /// </summary>
    /// <param name="type">The type containing the patch methods.</param>
    /// <remarks>
    ///     The type does not need a <see cref="PatchAttribute" /> when patched directly. Inherited methods are not
    ///     processed.
    /// </remarks>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle PatchAll(Type type)
    {
        var handle = RegisterAll(type);
        Apply();
        return handle;
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
    /// <returns>A handle that owns every patch registered for <paramref name="method" /> by this call.</returns>
    internal static PatchHandle Register(MethodInfo method)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessMethod(method, handle.id);
        return handle;
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
    /// <returns>A handle that owns every patch registered for <paramref name="method" /> by this call.</returns>
    internal static PatchHandle Register(MethodInfo method, params IEnumerable<MethodBase> targets)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessMethod(method, targets, handle.id);
        return handle;
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
    /// <returns>A handle that owns every patch registered for <paramref name="method" /> by this call.</returns>
    internal static PatchHandle Register(
        MethodInfo method,
        PatchType patchType,
        MemberInfo? innerTarget = null,
        MemberType innerMemberType = MemberType.Any,
        PatchOptions options = PatchOptions.Default,
        params IEnumerable<MethodBase> targets)
    {
        PatchHandle handle = new PatchHandle();
        registry.ProcessMethod(method, patchType, innerTarget, innerMemberType, options, targets, method.DeclaringType!.FullName, handle.id);
        return handle;
    }

    /// <summary>
    ///     Applies the patch described by an attributed method.
    /// </summary>
    /// <param name="method">The method that defines the patch.</param>
    /// <remarks>
    ///     Attributes on the method and its declaring type are both considered. The declaring type does not need a
    ///     <see cref="PatchAttribute" /> when the method is patched directly.
    /// </remarks>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle Patch(MethodInfo method)
    {
        var handle = Register(method);
        Apply();
        return handle;
    }

    /// <summary>
    ///     Applies a configured patch.
    /// </summary>
    /// <param name="patch">The patch to apply.</param>
    /// <returns>A handle for removing the patch.</returns>
    public static PatchHandle Patch(PatchConfig patch)
    {
        return Patch([patch]);
    }

    /// <summary>
    ///     Applies the same configured patch to several methods or constructors.
    /// </summary>
    /// <param name="patch">The patch to apply. The supplied methods replace any target already in the configuration.</param>
    /// <param name="methods">The methods and constructors to patch.</param>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle Patch(PatchConfig patch, params IEnumerable<MethodBase> methods)
    {
        return Patch(methods.Select(patch.Of));
    }

    /// <summary>
    ///     Applies several configured patches to the same method or constructor.
    /// </summary>
    /// <param name="method">The method or constructor to patch. It replaces any target in the configurations.</param>
    /// <param name="patches">The patches to apply.</param>
    /// <returns>A handle for removing the patches added by this call.</returns>
    public static PatchHandle Patch(MethodBase method, params IEnumerable<PatchConfig> patches)
    {
        return Patch(patches.Select(patch => patch.Of(method)));
    }

    /// <summary>
    ///     Applies several configured patches as one group.
    /// </summary>
    /// <param name="patches">
    ///     The patches to apply. Each configuration must specify a patch type, patch method, and target.
    /// </param>
    /// <returns>A handle for removing the group.</returns>
    /// <remarks>
    ///     Patches in the group can share state.
    /// </remarks>
    public static PatchHandle Patch(params IEnumerable<PatchConfig> patches)
    {
        PatchHandle handle = new PatchHandle();
        var stateKey = Guid.NewGuid().ToString();
        foreach (var patch in patches)
            registry.ProcessPatch(patch, stateKey, handle.id);
        Apply();
        return handle;
    }

    /// <summary>
    ///     Removes patches added by an earlier patching call.
    /// </summary>
    /// <param name="handle">The handle returned by that call.</param>
    /// <remarks>
    ///     Patches added by other calls remain active, even when they patch the same methods.
    /// </remarks>
    public static void Unpatch(PatchHandle handle)
    {
        registry.Unpatch(handle.id);
        Apply();
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
    internal static void Apply()
    {
        registry.ApplyImpl(useTrampolines: true);
    }

    /// <summary>
    ///     Prepares all current patches immediately to avoid work when a patched method is first called.
    /// </summary>
    /// <remarks>
    ///     Patches are already active without this call. Use it during a convenient idle period when avoiding first-use
    ///     delay matters. Patches added afterward may require another call.
    /// </remarks>
    public static void ForceApply()
    {
        registry.ApplyImpl(useTrampolines: false);
        harmonyInterface.ResolveAllTrampolines();
    }

    /// <summary>
    ///     Removes every registered Disharmony patch and reapplies the affected targets.
    /// </summary>
    /// <remarks>
    ///     This does not remove patches installed independently through Harmony.
    /// </remarks>
    internal static void UnpatchAll()
    {
        registry.UnpatchAll();
        registry.ApplyImpl(useTrampolines: true);
    }
}
