namespace Disharmony;

internal enum Scope
{
    /// <summary>
    ///     Represents access to parameters or results of the inner method.
    /// </summary>
    Inner,

    /// <summary>
    ///     Represents access to parameters or results of the outer method.
    /// </summary>
    Outer,
}

internal enum BindingType
{
    /// <summary>
    ///     Access to a method call's formal parameter.
    /// </summary>
    Parameter,

    /// <summary>
    ///     Access to a method call's instance parameter.
    /// </summary>
    Instance,

    /// <summary>
    ///     Access to the result of calling the inner method.
    /// </summary>
    Result,

    /// <summary>
    ///     Access to a local state variable.
    /// </summary>
    State,
}

internal struct ParameterBinding
{
    /// <summary>
    ///     The <see cref="ParameterInfo" /> of the parameter from the patch function.
    /// </summary>
    /// <remarks>
    ///     This is used to get the parameter's type, as well as its name for logging.
    /// </remarks>
    public ParameterInfo Parameter;

    /// <summary>
    ///     Whether this applies to the outer method (caller) or inner method (target).
    /// </summary>
    public Scope Scope;

    /// <summary>
    ///     The type of binding.
    /// </summary>
    public BindingType BindingType;

    /// <summary>
    ///     Depending on <see cref="BindingType" /> and <see cref="Scope" /> this can be either a local variable index or an
    ///     index into the caller or target argument lists.
    /// </summary>
    /// <remarks>
    ///     For argument lists of instance methods, index 0 is the instance and formal arguments start from index 1; for static
    ///     methods, formal arguments start from index 0.
    /// </remarks>
    public int Index;

    public FieldInfo[]? Fields;
}

internal struct PatchInfo
{
    public required MemberInfo? inner;
    public required MethodInfo outer;
    public required MethodInfo patchMethod;
    public required PatchType patchType;
    public required ParameterBinding[] parameters;
    public required bool inline;
    public bool debug;

    public bool HasBindingType(BindingType bindingType) => parameters.Any(p => p.BindingType == bindingType);
}

internal class PatchRegistry
{
    public static readonly PatchRegistry Instance = new();

    // When another lock is also needed, this must be taken after Autopatcher's apply lock
    // and before Harmony's lock.
    internal readonly object SyncRoot = new();
    public readonly HashSet<MethodInfo> MethodsToUpdate = [];
    public Dictionary<MethodInfo, List<PatchInfo>> PatchesByMethod = new();

    private PatchRegistry()
    {
    }

    private List<PatchInfo> Patches { get; } = [];

    public void ProcessAssembly(Assembly assembly)
    {
        lock (SyncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                var harmonyAttribute = type.GetCustomAttribute<HarmonyPatch>();
                if (harmonyAttribute == null)
                    continue;

                ProcessType(type);
            }

            PatchesByMethod = Patches.GroupBy(patch => patch.outer).ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    public void ProcessAssembly(Assembly assembly, string? category)
    {
        lock (SyncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                var harmonyAttribute = type.GetCustomAttribute<HarmonyPatch>();
                if (harmonyAttribute == null)
                    continue;

                var patchCategory = type.GetCustomAttribute<HarmonyPatchCategory>()?.info.category;
                if (patchCategory != category)
                    continue;

                ProcessType(type);
            }

            PatchesByMethod = Patches.GroupBy(patch => patch.outer).ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    public void ProcessType(TypeInfo type)
    {
        lock (SyncRoot)
        {
            foreach (MethodInfo method in type.DeclaredMethods)
            {
                ProcessMethod(method);
            }
        }
    }

    public void ProcessMethod(MethodInfo method)
    {
        lock (SyncRoot)
        {
            try
            {
                var typeAttributes = method.DeclaringType?.GetCustomAttributes().ToList() ?? [];
                var methodAttributes = method.GetCustomAttributes();

                List<Attribute> attributes = [.. typeAttributes, .. methodAttributes];

                var defaultTargetType = attributes.OfType<HarmonyPatch>().Select(p => p.info.declaringType)
                    .FirstOrDefault(t => t is not null);
                var patchTypeAttribute = attributes.OfType<PatchTypeAttribute>().SingleOrDefault();
                var targetAttributes = attributes.OfType<TargetAttribute>().ToList();
                bool debug = attributes.OfType<DebugAttribute>().Any();
                bool inline = attributes.OfType<InlineAttribute>().Any();

                if (patchTypeAttribute == null)
                    return;

                PatchType patchType = patchTypeAttribute.patchType;

                MemberInfo? inner = patchTypeAttribute.memberName != null
                    ? ReflectionTools.GetMember(patchTypeAttribute.type, patchTypeAttribute.memberName, patchTypeAttribute.memberType,
                        patchTypeAttribute.parameterTypes, patchTypeAttribute.genericTypes)
                    : null;

                if (patchType is PatchType.InnerPrefix or PatchType.InnerPostfix && inner == null)
                    throw new InvalidOperationException($"{patchType} patch must have an inner target");

                foreach (var targetAttribute in targetAttributes)
                {
                    var patchedType = targetAttribute.type ?? defaultTargetType;
                    if (patchedType == null)
                        throw new NotSupportedException("No target type");

                    MethodInfo? outer = ReflectionTools.GetMember(patchedType, targetAttribute.methodName, targetAttribute.memberType,
                        targetAttribute.parameterTypes, targetAttribute.genericTypes) as MethodInfo;

                    if (outer == null)
                        throw new InvalidOperationException($"Couldn't locate method {targetAttribute.methodName}");

                    AddPatch(method, patchType, outer, inner, inline, debug);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error processing {method.FullName}", e);
            }
        }
    }

    private void AddPatch(
        MethodInfo method,
        PatchType patchType,
        MethodInfo outer,
        MemberInfo? inner,
        bool inline = false,
        bool debug = false)
    {
        if (patchType is PatchType.InnerPrefix or PatchType.InnerPostfix)
        {
            var iterator = outer.GetIteratorImplementation();
            if (iterator != null)
                outer = iterator;
        }

        var parameterBinder = new ParameterBinder(outer, inner);

        var arguments = method.GetParameters().Select(parameterBinder.BindParameter).ToArray();

        PatchInfo patch = new()
        {
            outer = outer,
            inner = inner,
            patchMethod = method,
            patchType = patchType,
            parameters = arguments,
            inline = inline,
            debug = debug,
        };
        Patches.Add(patch);

        MethodsToUpdate.Add(outer);

        if (!PatchesByMethod.TryGetValue(outer, out var patchList))
            patchList = PatchesByMethod[outer] = [];
        patchList.Add(patch);
    }
}
