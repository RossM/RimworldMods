namespace Disharmony;

public static partial class Autopatcher
{
    public enum PatchType
    {
        InnerPrefix,
        InnerPostfix,
    }

    private enum Scope
    {
        /// <summary>
        ///     Represents access to parameters or results of the inner (target) method.
        /// </summary>
        Inner,

        /// <summary>
        ///     Represents access to parameters or results of the outer (caller) method.
        /// </summary>
        Outer,
    }

    private enum BindingType
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

    private struct ParameterBinding
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

    private class StateBuilder<TStateKey>
    {
        private readonly Dictionary<TStateKey, (int index, Type type)> stateMap = new();
        public readonly List<Type> LocalTypes = [];

        public int GetOrAddStateLocal(TStateKey stateKey, Type localType, MethodInfo method)
        {
            if (localType.IsByRef)
                localType = localType.GetElementType();

            if (stateMap.TryGetValue(stateKey, out var tuple))
            {
                (int index, Type existingType) = tuple;

                if (existingType == localType)
                    return index;

                throw new ArgumentException(
                    $"{method.DeclaringType?.FullName}.{method.Name} declares __state of type {localType} which conflicts with existing type {existingType}");
            }

            int newIndex = LocalTypes.Count;
            stateMap.Add(stateKey, (newIndex, localType));
            LocalTypes.Add(localType);
            return newIndex;
        }

        public InstructionMatcher.Rule BuildRule()
        {
            List<CodeInstruction> output = [];

            for (int index = 0; index < LocalTypes.Count; index++)
            {
                EmitInitializer(LocalTypes[index], index, output);
            }

            return new InstructionMatcher.Rule
            {
                Mode = InstructionMatcher.OutputMode.MethodPrefix,
                Output = output.ToArray(),
                Name = "state variable initialization",
            };
        }
    }

    private struct PatchInfo
    {
        public required MemberInfo target;
        public required MethodInfo caller;
        public required MethodInfo patchMethod;
        public required PatchType patchType;
        public required ParameterBinding[] parameters;
        public bool debug;

        public bool HasBindingType(BindingType bindingType) => parameters.Any(p => p.BindingType == bindingType);
    }

    private static readonly Dictionary<MethodInfo, int> patchVersions = new();

    private static readonly AssemblyBuilder assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" }, AssemblyBuilderAccess.RunAndSave);

    private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

    private static int IncrementVersion(MethodInfo method)
    {
        if (!patchVersions.TryGetValue(method, out int version))
            version = 0;
        return patchVersions[method] = version + 1;
    }

    private static void EmitInitializer(Type type, int localIndex, List<CodeInstruction> codeInstructions)
    {
        if (type.IsByRef)
            throw new NotImplementedException($"IsByRef targetType {type}");

        if (type.IsClass)
        {
            codeInstructions.Add(new(OpCodes.Ldnull));
            codeInstructions.Add(CodeInstruction.StoreLocal(localIndex));
        }
        else if (type.IsStruct())
        {
            codeInstructions.Add(CodeInstruction.LoadLocalAddress(localIndex));
            codeInstructions.Add(new(OpCodes.Initobj, type));
        }
        else if (type.IsValueType)
        {
            if (type == typeof(float))
                codeInstructions.Add(new(OpCodes.Ldc_R4, (float)0));
            else if (type == typeof(double))
                codeInstructions.Add(new(OpCodes.Ldc_R8, (double)0));
            else if (type == typeof(long) || type == typeof(ulong))
                codeInstructions.Add(new(OpCodes.Ldc_I8, (long)0));
            else
                codeInstructions.Add(new(OpCodes.Ldc_I4_0));

            codeInstructions.Add(CodeInstruction.StoreLocal(localIndex));
        }
        else
            throw new NotImplementedException($"targetType {type}");
    }

    public static void PatchAll(Harmony harmony, Assembly assembly)
    {
        var worker = new PatchWorker(assembly);

        worker.CollectPatches();

        foreach (MethodInfo patchedMethod in worker.TargetMethods)
        {
            try
            {
                MethodInfo patchTranspiler = worker.CreatePatchTranspiler(patchedMethod);
                bool debug = worker.ShouldDebug(patchedMethod);

                RunPatch(patchedMethod, patchTranspiler, Priority.Normal, debug);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
            }
        }

        void RunPatch(MethodInfo patchedMethod, MethodInfo patchTranspiler, int priority, bool debug)
        {
            bool oldForceDebug = InstructionMatcher.forceDebug;

            try
            {
                harmony.Patch(patchedMethod, transpiler: new(patchTranspiler, priority: priority) { debug = debug });
            }
            catch (Exception)
            {
                // Rerun with debug on so we see what went wrong
                InstructionMatcher.forceDebug = true;
                harmony.Patch(patchedMethod, transpiler: new(patchTranspiler, priority: priority) { debug = true });
            }
            finally
            {
                InstructionMatcher.forceDebug = oldForceDebug;
            }
        }
    }

    private static ParameterBinding BindParameter(
        ParameterInfo parameter,
        MethodInfo caller,
        MemberInfo target,
        StateBuilder<Type> stateBuilder)
    {
        var parameterName = parameter.Name;

        switch (parameterName)
        {
            case "__caller":
            {
                if (caller.IsStatic)
                    throw new ArgumentException("__caller argument cannot be used with static outer method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer };
            }

            case "__instance":
            {
                if (target is MethodInfo { IsStatic: true } or PropertyInfo { GetMethod.IsStatic: true } or FieldInfo { IsStatic: true })
                    throw new ArgumentException("__instance argument cannot be used with static inner method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner };
            }

            case "__result":
            {
                if (target is MethodInfo info && info.ReturnType.IsVoid())
                    throw new ArgumentException("__result argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = Scope.Inner };
            }

            case "__state":
            {
                int index = stateBuilder.GetOrAddStateLocal(caller.DeclaringType, parameter.ParameterType, caller);
                return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer, Index = index };
            }

            case not null when parameterName.StartsWith("___"):
            {
                var fieldName = parameterName[3..];

                // Look in target instance fields
                if (target is FieldInfo { IsStatic: false } or MethodInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false })
                {
                    var field = target.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
                }

                // Look in target instance fields
                if (caller is { IsStatic: false })
                {
                    var field = caller.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
                }

                throw new ArgumentException($"Field not found: {fieldName}");
            }

            default:
            {
                // Look in target parameters
                if (target is MethodInfo targetMethod)
                {
                    int index = Array.FindIndex(targetMethod.GetParameters(), p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        if (!targetMethod.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
                    }
                }

                // Look in caller parameters
                {
                    int index = Array.FindIndex(caller.GetParameters(), p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        if (!caller.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Outer, Index = index };
                    }
                }

                // Look in closure fields
                if (target is MethodInfo targetMethod2)
                {
                    int closureIndex = Array.FindLastIndex(targetMethod2.GetParameters(), p => IsClosureType(p.ParameterType));
                    if (closureIndex >= 0)
                    {
                        var type = targetMethod2.GetParameters()[closureIndex].ParameterType;
                        if (type.IsByRef)
                            type = type.GetElementType();

                        var field = type.GetField(parameterName, AccessTools.all);

                        if (!targetMethod2.IsStatic)
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

    private static MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName, bool debug)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

        FieldBuilder matchersField = typeBuilder.DefineField("matchers", typeof(InstructionMatcher[]),
            FieldAttributes.Public | FieldAttributes.Static);
        FieldBuilder debugField = typeBuilder.DefineField("debug", typeof(bool),
            FieldAttributes.Public | FieldAttributes.Static);

        MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
            typeof(List<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
        ILGenerator generator = methodBuilder.GetILGenerator();

        MethodInfo matchAndReplace = SymbolExtensions.GetMethodInfo(() => InstructionMatcher.RunMatchers);

        generator.Emit(OpCodes.Ldsfld, matchersField);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldsfld, debugField);
        generator.Emit(OpCodes.Call, matchAndReplace);
        generator.Emit(OpCodes.Ret);

        Type type = typeBuilder.CreateType();
        type.GetField(matchersField.Name).SetValue(null, matchers);
        type.GetField(debugField.Name).SetValue(null, debug);
        return type.GetMethod(methodBuilder.Name);
    }

    /// <summary>
    ///     This creates a rule that replaces all calls of a given method with calls of a given other method. The
    ///     new method's parameters will be filled with the values of the old method's parameters that have the
    ///     same name. If the old method doesn't have a parameter with that name, the parameters of the method
    ///     containing the call being modified are checked, and used if they match.
    ///     You can also use __instance to match the instance the method was invoked on, and __caller to match
    ///     the instance the calling method was invoked on.
    ///     If there isn't a parameter with a matching name, this will fall back to trying to match based
    ///     on parameter type, but this will give a warning.
    /// </summary>
    /// <param name="oldMember"></param>
    /// <param name="newMember"></param>
    /// <param name="minMatches"></param>
    /// <returns></returns>
    public static InstructionMatcher.Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember)
    {
        return new()
        {
            LateGenerator = (caller, _, generator) => RedirectRule_Core(generator, caller, oldMember, newMember, [], [], []),
        };
    }

    private static InstructionMatcher.Rule RedirectRule_Core(
        ILGenerator generator,
        MethodBase caller,
        MemberInfo target,
        MethodInfo? replacementTarget,
        List<PatchInfo> prefixes,
        List<PatchInfo> postfixes,
        List<Type> localTypes)
    {
        var methodPatchWorker = new RuleBuilder(generator, caller, target, replacementTarget, prefixes, postfixes, localTypes);

        return methodPatchWorker.BuildRule();
    }
}
