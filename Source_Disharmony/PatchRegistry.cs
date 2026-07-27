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

    /// <summary>
    ///     Gets a delegate to the base method of the method.
    /// </summary>
    BaseMethod,
}

internal class ParameterBinding
{
    /// <summary>
    ///     The <see cref="ParameterInfo" /> of the parameter from the patch function.
    /// </summary>
    /// <remarks>
    ///     This is used to get the parameter's type, as well as its name for logging.
    /// </remarks>
    public required ParameterInfo parameter;

    /// <summary>
    ///     Whether this applies to the outer method (caller) or inner method (target).
    /// </summary>
    public required Scope scope;

    /// <summary>
    ///     The type of binding.
    /// </summary>
    public required BindingType bindingType;

    /// <summary>
    ///     Depending on <see cref="bindingType" /> and <see cref="scope" /> this can be either a local variable index or an
    ///     index into the caller or target argument lists.
    /// </summary>
    /// <remarks>
    ///     For argument lists of instance methods, index 0 is the instance and formal arguments start from index 1; for static
    ///     methods, formal arguments start from index 0.
    /// </remarks>
    public int index;

    public FieldInfo[]? fields;

    public string? stateKey;
}

internal struct PatchInfo
{
    public required object unpatchKey;
    public required Invocation inner;
    public required Invocation patch;
    public required PatchType patchType;
    public required ParameterBinding[] parameters;
    public bool inline;
    public bool debug;
    public bool optimize;
    public readonly bool HasBindingType(BindingType bindingType) => parameters.Any(p => p.bindingType == bindingType);
}

internal class PatchRegistry
{
    public static readonly PatchRegistry Instance = new();

    // When another lock is also needed, this must be taken after Autopatcher's apply lock
    // and before Harmony's lock.
    private readonly object syncRoot = new();
    private readonly HashSet<MethodBaseInvocation> methodsToUpdate = [];
    private readonly Dictionary<MethodBaseInvocation, List<PatchInfo>> patchesByMethod = [];

    private PatchRegistry() { }

    public List<PatchInfo> GetPatchesFor(MethodBaseInvocation method)
    {
        lock (syncRoot)
            return [.. patchesByMethod[method]];
    }

    public void ProcessAssembly(Assembly assembly)
    {
        lock (syncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                if (type.GetCustomAttribute<PatchAttribute>() != null || type.GetCustomAttribute<HarmonyPatch>() != null)
                    ProcessType(type);
            }
        }
    }

    public void ProcessAssembly(Assembly assembly, string? category)
    {
        lock (syncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                if (type.GetCustomAttribute<PatchAttribute>() == null && type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                if ((type.GetCustomAttribute<CategoryAttribute>()?.category ??
                     type.GetCustomAttribute<HarmonyPatchCategory>()?.info.category) != category)
                    continue;

                ProcessType(type);
            }
        }
    }

    public void ProcessType(TypeInfo type)
    {
        lock (syncRoot)
        {
            foreach (MethodInfo method in type.DeclaredMethods)
            {
                ProcessMethod(method);
            }
        }
    }

    public void ProcessMethod(MethodInfo method)
    {
        lock (syncRoot)
        {
            try
            {
                var typeAttributes = method.DeclaringType?.GetCustomAttributes().ToList() ?? [];
                var methodAttributes = method.GetCustomAttributes();

                List<Attribute> attributes = [.. typeAttributes, .. methodAttributes];

                var defaultTargetType =
                    attributes.OfType<PatchAttribute>().Select(p => p.type).FirstOrDefault(t => t is not null) ??
                    attributes.OfType<HarmonyPatch>().Select(p => p.info.declaringType).FirstOrDefault(t => t is not null);
                var patchTypeAttribute = attributes.OfType<PatchTypeAttribute>().SingleOrDefault();
                var targetAttributes = attributes.OfType<TargetAttribute>().ToList();
                bool debug = attributes.OfType<DebugAttribute>().Any();
                bool inline = attributes.OfType<InlineAttribute>().Any();
                bool optimize = attributes.OfType<OptimizeAttribute>().Any();

                if (patchTypeAttribute == null)
                    return;

                PatchType patchType = patchTypeAttribute.patchType;

                Invocation inner = GetInnerInvocation(patchTypeAttribute);

                foreach (var targetAttribute in targetAttributes)
                {
                    var patchedType = targetAttribute.type ?? defaultTargetType ??
                        throw new NotSupportedException("No target type");

                    List<MemberInfo> candidates = ReflectionTools.GetMembers(patchedType, targetAttribute.methodName,
                        targetAttribute.memberType, targetAttribute.parameterTypes, targetAttribute.genericTypes);

                    var nameForErrors = targetAttribute.memberType == MemberType.Constructor ? ".ctor" : targetAttribute.methodName;

                    switch (candidates.Count)
                    {
                        case > 1 when targetAttribute is not TargetsAttribute:
                            throw new AmbiguousMatchException($"{nameForErrors}: Ambiguous match");
                        case 0: throw new InvalidOperationException($"{nameForErrors}: Member not found");
                    }

                    foreach (var result in candidates)
                    {
                        MethodBase target = result as MethodBase ??
                                            throw new InvalidOperationException($"{nameForErrors}: Couldn't locate method");
                        AddPatch(method, patchType, target, inner, inline, debug, optimize);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error processing {method.FullName}", e);
            }
        }
    }

    public void ProcessMethod(MethodInfo method, IEnumerable<MethodBase> targets)
    {
        lock (syncRoot)
        {
            try
            {
                var typeAttributes = method.DeclaringType?.GetCustomAttributes().ToList() ?? [];
                var methodAttributes = method.GetCustomAttributes();

                List<Attribute> attributes = [.. typeAttributes, .. methodAttributes];

                var patchTypeAttribute = attributes.OfType<PatchTypeAttribute>().SingleOrDefault();
                bool debug = attributes.OfType<DebugAttribute>().Any();
                bool inline = attributes.OfType<InlineAttribute>().Any();
                bool optimize = attributes.OfType<OptimizeAttribute>().Any();

                if (patchTypeAttribute == null)
                    return;

                PatchType patchType = patchTypeAttribute.patchType;

                Invocation inner = GetInnerInvocation(patchTypeAttribute);

                foreach (var target in targets)
                {
                    AddPatch(method, patchType, target, inner, inline, debug, optimize);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error processing {method.FullName}", e);
            }
        }
    }

    public void ProcessMethod(
        MethodInfo method,
        PatchType patchType,
        MemberInfo? innerTarget,
        MemberType innerMemberType,
        PatchOptions options,
        IEnumerable<MethodBase> targets)
    {
        lock (syncRoot)
        {
            try
            {
                bool inline = options.HasFlag(PatchOptions.Inline);
                bool debug = options.HasFlag(PatchOptions.Debug);
                bool optimize = options.HasFlag(PatchOptions.Optimize);

                Invocation inner = patchType is PatchType.InnerPrefix or PatchType.InnerPostfix
                    ? InnerInvocation(innerTarget, innerMemberType)
                    : EmptyInvocation.Instance;

                foreach (var target in targets)
                {
                    AddPatch(method, patchType, target, inner, inline, debug, optimize);
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
            case { memberName: string } or { memberType: MemberType.Constructor }:
            {
                MemberInfo inner = ReflectionTools.GetMember(patchTypeAttribute.type, patchTypeAttribute.memberName,
                    patchTypeAttribute.memberType, patchTypeAttribute.parameterTypes, patchTypeAttribute.genericTypes);

                return InnerInvocation(inner, patchTypeAttribute.memberType);
            }
            default: throw new InvalidOperationException($"{patchTypeAttribute.patchType} patch must have an inner target");
        }
    }

    private static Invocation InnerInvocation(MemberInfo? inner, MemberType memberType)
    {
        return inner switch
        {
            FieldInfo field => memberType is MemberType.Setter
                ? new SetFieldInvocation(field)
                : new FieldInvocation(field),
            MethodInfo method => new MethodInvocation(method),
            ConstructorInfo constructor => new ConstructorInvocation(constructor),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void AddPatch(MethodInfo method, PatchType patchType, MethodBase target, Invocation inner, bool inline, bool debug, bool optimize)
    {
        if (target.IsGenericMethod)
            throw new InvalidOperationException($"{target.FullName}: Can't patch instantiated generic method");

        MethodBaseInvocation outer = target switch
        {
            MethodInfo outerMethod => new MethodInvocation(outerMethod),
            ConstructorInfo outerConstructor => new PatchableConstructorInvocation(outerConstructor),
            _ => throw new ArgumentOutOfRangeException(),
        };
        AddPatch(method, patchType, outer, inner, inline: inline, debug: debug, optimize: optimize);
    }

    private void AddPatch(
        MethodInvocation patchMethod,
        PatchType patchType,
        MethodBaseInvocation target,
        Invocation inner,
        bool inline = false,
        bool debug = false,
        bool optimize = false)
    {
        MethodBaseInvocation outer = target;
        bool isIterator = false;

        if (patchType is PatchType.InnerPrefix or PatchType.InnerPostfix && outer is MethodInvocation outerMethod)
        {
            var iterator = outerMethod.MethodInfo.GetIteratorImplementation();
            if (iterator != null)
            {
                outer = iterator;
                isIterator = true;
            }
        }

        var parameterBinder = new ParameterBinder(target, outer, inner, patchType, patchMethod.MethodInfo.DeclaringType!.FullName);

        var arguments = patchMethod.MethodInfo.GetParameters().Select(parameterBinder.Bind).ToArray();

        if (isIterator && arguments.Any(p => p.bindingType == BindingType.State))
            throw new NotSupportedException("State parameters are not supported for iterator state machine methods");

        PatchInfo patch = new()
        {
            unpatchKey = patchMethod.MethodInfo.Module.Assembly,
            inner = inner,
            patch = patchMethod,
            patchType = patchType,
            parameters = arguments,
            inline = inline,
            debug = debug,
            optimize = optimize,
        };

        methodsToUpdate.Add(outer);

        if (!patchesByMethod.TryGetValue(outer, out var patchList))
            patchList = patchesByMethod[outer] = [];
        patchList.Add(patch);
    }

    public void UnpatchAll(Assembly assembly)
    {
        lock (syncRoot)
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
        while (true)
        {
            lock (syncRoot)
            {
                if (methodsToUpdate.Count == 0)
                    return;

                var patchedMethod = methodsToUpdate.First();
                try
                {
                    var worker = new Autopatcher.PatchWorker(this, patchedMethod, useTrampolines);

                    worker.UpdateMethod();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
                }
                finally
                {
                    methodsToUpdate.Remove(patchedMethod);
                }
            }
        }
    }
}
