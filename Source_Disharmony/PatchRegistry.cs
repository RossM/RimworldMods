namespace Disharmony;

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

internal class ParameterBinding
{
    /// <summary>
    ///     The <see cref="ParameterInfo" /> of the parameter from the patch function.
    /// </summary>
    /// <remarks>
    ///     This is used to get the parameter's type, as well as its name for logging.
    /// </remarks>
    public required ParameterInfo Parameter;

    /// <summary>
    ///     Whether this applies to the outer method (caller) or inner method (target).
    /// </summary>
    public required Scope Scope;

    /// <summary>
    ///     The type of binding.
    /// </summary>
    public required BindingType BindingType;

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
    public required object unpatchKey;
    public required Invocation inner;
    public required Invocation patch;
    public required PatchType patchType;
    public required Type stateKey;
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
    private readonly object SyncRoot = new();
    private readonly HashSet<MethodInfo> methodsToUpdate = [];
    private readonly Dictionary<MethodInfo, List<PatchInfo>> patchesByMethod = new();

    private PatchRegistry() { }

    public List<PatchInfo> GetPatchesFor(MethodInfo method)
    {
        lock (SyncRoot)
            return patchesByMethod[method].ToList();
    }

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

                Invocation inner = GetInnerInvocation(patchTypeAttribute);

                foreach (var targetAttribute in targetAttributes)
                {
                    var patchedType = targetAttribute.type ?? defaultTargetType;
                    if (patchedType == null)
                        throw new NotSupportedException("No target type");

                    List<MemberInfo> candidates = ReflectionTools.GetMembers(patchedType, targetAttribute.methodName,
                        targetAttribute.memberType, targetAttribute.parameterTypes, targetAttribute.genericTypes);

                    switch (candidates.Count)
                    {
                        case > 1 when targetAttribute is not TargetsAttribute:
                            throw new AmbiguousMatchException($"Ambiguous match: {targetAttribute.methodName}");
                        case 0: throw new InvalidOperationException($"Member not found: {targetAttribute.methodName}");
                    }

                    foreach (var result in candidates)
                    {
                        MethodInfo? target = result as MethodInfo;

                        if (target == null)
                            throw new InvalidOperationException($"Couldn't locate method {targetAttribute.methodName}");
                        if (target.IsGenericMethod)
                            throw new InvalidOperationException($"Can't patch instantiated generic method");

                        AddPatch(method, patchType, target, inner, inline, debug);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error processing {method.FullName}", e);
            }
        }
    }

    private static Invocation GetInnerInvocation(PatchTypeAttribute patchTypeAttribute)
    {
        if (patchTypeAttribute.patchType is not (PatchType.InnerPrefix or PatchType.InnerPostfix))
            return EmptyInvocation.Instance;

        switch (patchTypeAttribute)
        {
            case InnerPostfixConstantAttribute { value: int value }: return new ConstantIntInvocation(value);
            case InnerPostfixConstantAttribute { value: long value }: return new ConstantLongInvocation(value);
            case InnerPostfixConstantAttribute { value: float value }: return new ConstantFloatInvocation(value);
            case InnerPostfixConstantAttribute { value: double value }: return new ConstantDoubleInvocation(value);
            case InnerPostfixConstantAttribute { value: string value }: return new ConstantStringInvocation(value);
            case { memberName: string memberName }:
            {
                MemberInfo inner = ReflectionTools.GetMember(patchTypeAttribute.type, patchTypeAttribute.memberName, patchTypeAttribute.memberType,
                    patchTypeAttribute.parameterTypes, patchTypeAttribute.genericTypes);

                return Invocation.Create(inner);
            }
            default: throw new InvalidOperationException($"{patchTypeAttribute.patchType} patch must have an inner target");
        }
    }

    private void AddPatch(
        MethodInvocation patchMethod,
        PatchType patchType,
        MethodInvocation target,
        Invocation inner,
        bool inline = false,
        bool debug = false)
    {
        MethodInvocation outer = target;
        bool isIterator = false;

        if (patchType is PatchType.InnerPrefix or PatchType.InnerPostfix)
        {
            var iterator = outer.MethodInfo.GetIteratorImplementation();
            if (iterator != null)
            {
                outer = iterator;
                isIterator = true;
            }
        }

        var parameterBinder = new ParameterBinder(target, outer, inner, patchType);

        var arguments = patchMethod.MethodInfo.GetParameters().Select(parameterBinder.Bind).ToArray();

        if (isIterator && arguments.Any(p => p.BindingType == BindingType.State))
            throw new NotSupportedException("State parameters are not supported for iterator state machine methods");

        PatchInfo patch = new()
        {
            unpatchKey = patchMethod.MethodInfo.Module.Assembly,
            inner = inner,
            patch = patchMethod,
            patchType = patchType,
            stateKey = patchMethod.MethodInfo.DeclaringType,
            parameters = arguments,
            inline = inline,
            debug = debug,
        };

        methodsToUpdate.Add(outer.MethodInfo);

        if (!patchesByMethod.TryGetValue(outer.MethodInfo, out var patchList))
            patchList = patchesByMethod[outer.MethodInfo] = [];
        patchList.Add(patch);
    }

    public void UnpatchAll(Assembly assembly)
    {
        lock (SyncRoot)
        {
            foreach (var kvp in patchesByMethod)
            {
                var outer = kvp.Key;
                var patchList = kvp.Value;

                int count = patchList.RemoveAll(p => ReferenceEquals(p.unpatchKey, assembly));
                if (count > 0)
                    methodsToUpdate.Add(outer);
            }
        }
    }

    public void ApplyImpl(bool useTrampolines)
    {
        lock (SyncRoot)
        {
            foreach (MethodInfo patchedMethod in methodsToUpdate)
            {
                try
                {
                    var worker = new Autopatcher.PatchWorker(this, patchedMethod, useTrampolines);

                    worker.UpdateMethod();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
                }
            }

            methodsToUpdate.Clear();
        }
    }
}
