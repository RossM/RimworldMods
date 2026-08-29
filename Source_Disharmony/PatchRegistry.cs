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
    ///     Gets a delegate based on a given MethodInfo.
    /// </summary>
    Delegate,
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

    public LocalTrackerBuilder? local;

    public FieldInfo[]? fields;

    public string? stateKey;

    public MethodBase? methodInfo;

    public bool useVirtualDispatch;
}

internal struct PatchInfo
{
    public required int unpatchKey;
    public required Invocation inner;
    public required Invocation patch;
    public required PatchType patchType;
    public required ParameterBinding[] parameters;
    public required PatchOptions options;
    public readonly bool Inline => options.HasFlag(PatchOptions.Inline);
    public readonly bool Debug => options.HasFlag(PatchOptions.Debug);
    public readonly bool Optimize => options.HasFlag(PatchOptions.Optimize);
    public readonly bool HasBindingType(BindingType bindingType) => parameters.Any(p => p.bindingType == bindingType);
}

internal class PatchRegistry
{
    public static readonly PatchRegistry Instance = new();

    // When another lock is also needed, this must be taken after Autopatcher's apply lock
    // and before Harmony's lock.
    private readonly object syncRoot = new();
    private readonly HashSet<MethodBaseInvocation> methodsToUpdate = [];
    private readonly MultiDictionary<MethodBaseInvocation, PatchInfo> patchesByMethod = [];
    private readonly Dictionary<int, HashSet<MethodBaseInvocation>> methodsByUnpatchKey = [];

    private PatchRegistry() { }

    public void ProcessAssembly(Assembly assembly, int unpatchKey)
    {
        lock (syncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                if (type.GetCustomAttribute<PatchAttribute>() != null || type.GetCustomAttribute<HarmonyPatch>() != null)
                    ProcessType(type, unpatchKey);
            }
        }
    }

    public void ProcessAssembly(Assembly assembly, string? category, int unpatchKey)
    {
        lock (syncRoot)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                if (type.GetCustomAttribute<PatchAttribute>() == null && type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                if ((type.GetCustomAttribute<CategoryAttribute>()?.Category ??
                     type.GetCustomAttribute<HarmonyPatchCategory>()?.info.category) != category)
                    continue;

                ProcessType(type, unpatchKey);
            }
        }
    }

    public void ProcessType(TypeInfo type, int unpatchKey)
    {
        lock (syncRoot)
        {
            foreach (MethodInfo method in type.DeclaredMethods)
                ProcessMethod(method, unpatchKey);
        }
    }

    public void ProcessMethod(MethodInfo method, int unpatchKey)
    {
        lock (syncRoot)
        {
            try
            {
                List<Attribute> attributes = GetAttributes(method);

                var defaultTargetType =
                    attributes.OfType<PatchAttribute>().Select(p => p.Type).FirstOrDefault(t => t is not null) ??
                    attributes.OfType<HarmonyPatch>().Select(p => p.info.declaringType).FirstOrDefault(t => t is not null);
                var patchTypeAttribute = attributes.OfType<PatchTypeAttribute>().SingleOrDefault();
                var innerAttribute = attributes.OfType<InnerAttributeBase>().SingleOrDefault();
                var targetAttributes = attributes.OfType<TargetAttribute>().ToList();
                var options = attributes.OfType<PatchOptionsAttribute>().FirstOrDefault()?.Options ?? PatchOptions.Default;

                if (patchTypeAttribute == null)
                    return;

                PatchType patchType = patchTypeAttribute.PatchType;

                Invocation inner = GetInnerInvocation(innerAttribute);

                foreach (var targetAttribute in targetAttributes)
                {
                    var patchedType = targetAttribute.Type ?? defaultTargetType ??
                        throw new PatchDefinitionException(method, "No target type");

                    List<MemberInfo> candidates = ReflectionTools.GetMembers(patchedType, targetAttribute.MethodName,
                        targetAttribute.MemberType, targetAttribute.ParameterTypes, targetAttribute.GenericTypes);

                    var nameForErrors = targetAttribute.MemberType == MemberType.Constructor ? ".ctor" : targetAttribute.MethodName;

                    switch (candidates.Count)
                    {
                        case > 1 when targetAttribute is not TargetsAttribute:
                            throw new AmbiguousMatchException($"{nameForErrors}: Ambiguous match");
                        case 0: throw new ReflectionException($"{nameForErrors}: Member not found");
                    }

                    foreach (var result in candidates)
                    {
                        MethodBase target = result as MethodBase ??
                                            throw new ReflectionException($"{nameForErrors}: Couldn't locate method");
                        AddPatch(new MethodInvocation(method), patchType, GetOuterInvocation(target), inner, options,
                            method.DeclaringType!.FullName, unpatchKey);
                    }
                }
            }
            catch (Exception e)
            {
                throw new PatchException($"Error processing {method.FullName}", e);
            }
        }
    }

    private static List<Attribute> GetAttributes(MethodInfo method)
    {
        var typeAttributes = method.DeclaringType?.GetCustomAttributes().ToList() ?? [];
        var methodAttributes = method.GetCustomAttributes();

        List<Attribute> attributes = [.. methodAttributes, .. typeAttributes];
        return attributes;
    }

    public void ProcessPatch(PatchConfig patch, string stateKey, int unpatchKey)
    {
        if (patch.PatchMethod is null)
            throw new ArgumentException("Patch method not set; call Patch.With()", nameof(patch));
        if (patch.Type is not { } patchType)
            throw new ArgumentException("Patch type not set; call Patch.Prefix or Patch.Postfix", nameof(patch));
        if (patch.Target is not MethodBaseInvocation targetInvocation)
            throw new ArgumentException("Patch target not set; call Patch.Of()", nameof(patch));

        lock (syncRoot)
        {
            try
            {
                AddPatch(new MethodInvocation(patch.PatchMethod), patchType, targetInvocation, patch.InnerTarget, patch.Options, stateKey,
                    unpatchKey);
            }
            catch (Exception e)
            {
                throw new PatchException($"Error processing {patch.PatchMethod.FullName}", e);
            }
        }
    }

    private static Invocation GetInnerInvocation(InnerAttributeBase? patchTypeAttribute)
    {
        if (patchTypeAttribute == null)
            return EmptyInvocation.Instance;

        switch (patchTypeAttribute)
        {
            case InnerConstantAttribute { Value: int value }: return new ConstantIntInvocation(value);
            case InnerConstantAttribute { Value: long value }: return new ConstantLongInvocation(value);
            case InnerConstantAttribute { Value: float value }: return new ConstantFloatInvocation(value);
            case InnerConstantAttribute { Value: double value }: return new ConstantDoubleInvocation(value);
            case InnerConstantAttribute { Value: string value }: return new ConstantStringInvocation(value);
            case InnerAttribute innerMember:
            {
                MemberInfo inner = ReflectionTools.GetMember(innerMember.Type, innerMember.MemberName,
                    innerMember.MemberType, innerMember.ParameterTypes, innerMember.GenericTypes);

                return GetInnerInvocation(inner, innerMember.MemberType);
            }
            default: throw new InvalidOperationException();
        }
    }

    private static Invocation GetInnerInvocation(MemberInfo? inner, MemberType memberType)
    {
        return inner switch
        {
            FieldInfo field => memberType is MemberType.Setter
                ? new SetFieldInvocation(field)
                : new GetFieldInvocation(field),
            MethodInfo method => new MethodInvocation(method),
            ConstructorInfo constructor => new InnerConstructorInvocation(constructor),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static MethodBaseInvocation GetOuterInvocation(MethodBase target)
    {
        return target switch
        {
            MethodInfo outerMethod => new MethodInvocation(outerMethod),
            ConstructorInfo outerConstructor => new OuterConstructorInvocation(outerConstructor),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void AddPatch(
        MethodInvocation patchMethod,
        PatchType patchType,
        MethodBaseInvocation target,
        Invocation inner,
        PatchOptions options,
        string stateGroupKey,
        int unpatchKey)
    {
        Validate(patchType, patchMethod.MethodInfo, target.MethodBase);

        MethodBaseInvocation outer = target;
        bool isIterator = false;

        if (inner is not EmptyInvocation && outer is MethodInvocation outerMethod)
        {
            var iterator = outerMethod.MethodInfo.GetIteratorImplementation();
            if (iterator != null)
            {
                outer = new MethodInvocation(iterator);
                isIterator = true;
            }
        }

        var parameterBinder = new ParameterBinder(target, outer, inner, patchType, stateGroupKey);

        var arguments = patchMethod.MethodInfo.GetParameters().Select(parameterBinder.Bind).ToArray();

        if (isIterator && arguments.Any(p => p.bindingType == BindingType.State))
            throw new NotSupportedException("State parameters are not supported for iterator state machine methods");

        PatchInfo patch = new()
        {
            unpatchKey = unpatchKey,
            inner = inner,
            patch = patchMethod,
            patchType = patchType,
            parameters = arguments,
            options = options,
        };

        methodsToUpdate.Add(outer);
        patchesByMethod.Add(outer, patch);

        if (!methodsByUnpatchKey.TryGetValue(unpatchKey, out var methodSet))
            methodSet = methodsByUnpatchKey[unpatchKey] = [];
        methodSet.Add(outer);
    }

    private static void Validate(PatchType patchType, MethodInfo method, MethodBase target)
    {
        if (method.ContainsGenericParameters)
            throw new PatchDefinitionException(method, "Generic patch functions are not supported");
        if (!method.IsStatic)
            throw new PatchDefinitionException(method, "Patch methods must be static");
        switch (patchType)
        {
            case PatchType.Prefix:
            {
                if (method.ReturnType != typeof(void) && method.ReturnType != typeof(bool))
                    throw new PatchDefinitionException(method, "Prefix must return 'bool' or 'void'");
                break;
            }
            case PatchType.Postfix:
            {
                if (method.ReturnType != typeof(void))
                    throw new PatchDefinitionException(method, "Postfix must return 'void'");
                break;
            }
            default: throw new ArgumentOutOfRangeException(nameof(patchType), patchType, null);
        }

        if (target.IsGenericMethod)
            throw new PatchDefinitionException(method, "Can't patch instantiated generic method");
    }

    public void UnpatchAll()
    {
        lock (syncRoot)
        {
            foreach (var group in patchesByMethod)
            {
                if (group.Any())
                    methodsToUpdate.Add(group.Key);
            }

            patchesByMethod.Clear();
            methodsByUnpatchKey.Clear();
        }
    }

    public void Unpatch(int unpatchKey)
    {
        lock (syncRoot)
        {
            if (!methodsByUnpatchKey.TryGetValue(unpatchKey, out var methods))
                return;

            foreach (var method in methods)
            {
                int count = patchesByMethod.RemoveAll(method, p => p.unpatchKey == unpatchKey);
                if (count > 0)
                    methodsToUpdate.Add(method);
            }

            methodsByUnpatchKey.Remove(unpatchKey);
        }
    }

    public void ApplyPendingChanges(bool useTrampolines)
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
                    IReadOnlyList<PatchInfo> patches = patchesByMethod[patchedMethod];

                    if (patches.Count == 0)
                    {
                        Patcher.Harmony.Unpatch(patchedMethod.MethodBase);
                    }
                    else
                    {
                        Ruleset ruleset = RulesetGenerator.MakeRuleset(patchedMethod, patches);

                        bool debug = patches.Any(p => p.Debug);
                        bool optimize = patches.Any(p => p.Optimize);

                        Patcher.Harmony.ApplyPatch(patchedMethod, ruleset, useTrampolines, debug, optimize);
                    }
                }
                catch (Exception e)
                {
                    throw new RuntimePatchException($"Error patching {patchedMethod.FullName}", e);
                }
                finally
                {
                    methodsToUpdate.Remove(patchedMethod);
                }
            }
        }
    }
}
