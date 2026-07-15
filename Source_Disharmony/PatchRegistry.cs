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
    public required MemberInfo inner;
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
    public HashSet<MethodInfo> MethodsToUpdate = new();
    public Dictionary<MethodInfo, List<PatchInfo>> PatchesByMethod = new();
    private List<PatchInfo> Patches { get; } = [];

    public void CollectPatches(Assembly assembly)
    {
        foreach (TypeInfo type in assembly.DefinedTypes)
        {
            var harmonyAttribute = (HarmonyPatch?)Attribute.GetCustomAttribute(type, typeof(HarmonyPatch));
            if (harmonyAttribute == null)
                continue;

            foreach (MethodInfo method in type.DeclaredMethods)
            {
                try
                {
                    var infixTargetAttribute
                        = (PatchTypeAttribute?)Attribute.GetCustomAttribute(method, typeof(InnerPrefixAttribute)) ??
                          (PatchTypeAttribute?)Attribute.GetCustomAttribute(method, typeof(InnerPostfixAttribute));
                    var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(TargetAttribute))
                        .Cast<TargetAttribute>().ToArray();
                    bool debug = Attribute.GetCustomAttribute(method, typeof(DebugAttribute)) != null;
                    bool inline = Attribute.GetCustomAttribute(method, typeof(InlineAttribute)) != null;

                    if (infixTargetAttribute == null)
                        continue;

                    MemberInfo? inner = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                        infixTargetAttribute.parameterTypes, infixTargetAttribute.genericTypes);
                    if (inner == null)
                        throw new InvalidOperationException("null wrapped member");

                    FileLog.Log($"# CollectPatches: {method.FullName}");

                    foreach (var infixPatchAttribute in infixPatchAttributes)
                    {
                        var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                        MethodInfo? outer = (MethodInfo?)GetMember(patchedType, infixPatchAttribute.methodName,
                            infixPatchAttribute.parameterTypes, infixPatchAttribute.genericTypes);
                        if (outer == null)
                            throw new InvalidOperationException("null target method");

                        var arguments = method.GetParameters().Select(param => BindParameter(param, outer, inner))
                            .ToArray();

                        Patches.Add(new()
                        {
                            outer = outer,
                            inner = inner,
                            patchMethod = method,
                            patchType = infixTargetAttribute.patchType,
                            parameters = arguments,
                            inline = inline,
                            debug = debug,
                        });

                        MethodsToUpdate.Add(outer);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error processing {type}:{method}", e);
                }
            }
        }

        PatchesByMethod = Patches.GroupBy(patch => patch.outer).ToDictionary(g => g.Key, g => g.ToList());
    }

    private static ParameterBinding BindParameter(
        ParameterInfo parameter,
        MethodInfo outer,
        MemberInfo inner)
    {
        var parameterName = parameter.Name;

        switch (parameterName)
        {
            case "__caller":
            {
                if (outer.IsStatic)
                    throw new ArgumentException("__caller argument cannot be used with static outer method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer };
            }

            case "__instance":
            {
                if (inner is MethodInfo { IsStatic: true } or PropertyInfo { GetMethod.IsStatic: true } or FieldInfo { IsStatic: true })
                    throw new ArgumentException("__instance argument cannot be used with static inner method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner };
            }

            case "__result":
            {
                if (inner is MethodInfo info && info.ReturnType.IsVoid())
                    throw new ArgumentException("__result argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = Scope.Inner };
            }

            case "__state":
            {
                return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };
            }

            case not null when parameterName.StartsWith("___"):
            {
                var fieldName = parameterName[3..];

                // Look in target instance fields
                if (inner is FieldInfo { IsStatic: false } or MethodInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false })
                {
                    var field = inner.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
                }

                // Look in target instance fields
                if (outer is { IsStatic: false })
                {
                    var field = outer.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
                }

                throw new ArgumentException($"Field not found: {fieldName}");
            }

            default:
            {
                // Look in target parameters
                if (inner is MethodInfo innerMethod)
                {
                    int index = Array.FindIndex(innerMethod.GetParameters(), p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        if (!innerMethod.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
                    }
                }

                // Look in caller parameters
                {
                    int index = Array.FindIndex(outer.GetParameters(), p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        if (!outer.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Outer, Index = index };
                    }
                }

                // Look in closure fields
                if (inner is MethodInfo innerMethod2)
                {
                    int closureIndex = Array.FindLastIndex(innerMethod2.GetParameters(), p => IsClosureType(p.ParameterType));
                    if (closureIndex >= 0)
                    {
                        var type = innerMethod2.GetParameters()[closureIndex].ParameterType;
                        if (type.IsByRef)
                            type = type.GetElementType();

                        var field = type.GetField(parameterName, AccessTools.all);

                        if (!innerMethod2.IsStatic)
                            closureIndex++;

                        if (field != null)
                            return new()
                            {
                                Parameter = parameter,
                                BindingType = BindingType.Parameter,
                                Scope = Scope.Inner,
                                Index = closureIndex,
                                Fields = [field],
                            };
                    }
                }

                throw new ArgumentException($"Argument not found: {parameterName}");
            }
        }
    }

    private static bool IsClosureType(Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType();

        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
    }

    private static MemberInfo? GetMember(Type type, string memberName, Type[]? parameterTypes, Type[]? genericTypes)
    {
        string[] nameParts = memberName.Split(':');
        for (int i = 0; i < nameParts.Length - 1; i++)
            type = AccessTools.InnerTypes(type).First(type1 => type1.Name.Contains(nameParts[i]));
        memberName = nameParts[^1];

        if (parameterTypes == null && genericTypes == null)
        {
            if (type.GetField(memberName, AccessTools.all) is { } field)
                return field;
            if (type.GetProperty(memberName, AccessTools.all) is { } property)
                return property.GetMethod;
        }

        return GetMethod(type, memberName, parameterTypes, genericTypes);
    }

    private static MethodInfo? GetMethod(Type type, string memberName, Type[]? parameterTypes, Type[]? genericTypes)
    {
        foreach (var method in type.GetMethods(AccessTools.all))
        {
            var curMethod = method;

            if (curMethod.Name != memberName)
                continue;

            if (curMethod.IsGenericMethod)
            {
                if (genericTypes is null)
                    continue;
                if (genericTypes.Length != curMethod.GetGenericArguments().Length)
                    continue;

                try
                {
                    curMethod = curMethod.MakeGenericMethod(genericTypes);
                }
                catch
                {
                    continue;
                }
            }
            else if (genericTypes is not null)
                continue;

            if (parameterTypes != null && !curMethod.GetParameters().Types().SequenceEqual(parameterTypes))
                continue;

            return curMethod;
        }

        return null;
    }
}
