namespace Disharmony;

public static class Autopatcher
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
        ///     Access to a field from a method call's formal parameter.
        /// </summary>
        ParameterField,

        /// <summary>
        ///     Access to aa field from a method call's instance parameter.
        /// </summary>
        InstanceField,

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

        /// <summary>
        ///     A <see cref="FieldInfo" /> used by <see cref="BindingType.ParameterField" /> and
        ///     <see cref="BindingType.InstanceField" />.
        /// </summary>
        public FieldInfo? Field;
    }

    private class RuleBuilder
    {
        private readonly Type[] callerParameterTypes;
        private readonly Type[] targetParameterTypes;
        private readonly int[] parameterToLocalIndex;
        private int resultLocalIndex = -1;
        private readonly Type targetType;

        private void EmitReplacement()
        {
            if (debug)
                LogDebugInfo();

            EmitPrelude();

            var prefixesUsingResult = prefixes.Where(patch => patch.parameters.Any(a => a.BindingType == BindingType.Result)).ToList();
            var postfixesUsingResult = postfixes.Where(patch => patch.parameters.Any(a => a.BindingType == BindingType.Result)).ToList();

            if (prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
            {
                resultLocalIndex = AddLocal(targetType);

                if (prefixesUsingResult.Count > 0)
                {
                    if (!prefixesUsingResult[0].parameters.Single(a => a.BindingType == BindingType.Result).Parameter.IsOut)
                        EmitInitializer(targetType, resultLocalIndex, output);
                }
            }

            Label? skipLabel = null;
            foreach (var prefix in prefixes)
            {
                MethodInfo patchMethod = prefix.patchMethod;
                foreach (var parameter in prefix.parameters)
                    EmitParameterValue(parameter);

                output.Add(CodeInstruction.Annotation($"{prefix.patchType} {patchMethod.FullName}"));
                output.Add(new(OpcodeFor(patchMethod), patchMethod));
                if (!patchMethod.ReturnType.IsVoid())
                {
                    output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
                }
            }

            for (int i = 0; i < targetParameterTypes.Length; i++)
            {
                EmitTargetParameter(targetParameterTypes[i], i);
            }

            if (replacementTarget != null)
                output.Add(new(OpCodes.Call, replacementTarget));
            else
                output.Add(new(OpcodeFor(target), target));

            if (skipLabel != null || postfixes.Count > 0)
            {
                if (resultLocalIndex >= 0)
                    output.Add(CodeInstruction.StoreLocal(resultLocalIndex));

                if (skipLabel is Label label)
                {
                    var branchTarget = new CodeInstruction(OpCodes.Nop);
                    branchTarget.labels.Add(label);
                    output.Add(branchTarget);
                }

                foreach (var postfix in postfixes)
                {
                    MethodInfo patchMethod = postfix.patchMethod;
                    foreach (var parameter in postfix.parameters)
                        EmitParameterValue(parameter);

                    output.Add(CodeInstruction.Annotation($"{postfix.patchType} {patchMethod.FullName}"));
                    output.Add(new(OpcodeFor(patchMethod), patchMethod));
                    if (!patchMethod.ReturnType.IsVoid())
                        output.Add(new(OpCodes.Pop));
                }

                if (resultLocalIndex >= 0)
                {
                    output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
                }
            }
        }

        private void LogDebugInfo()
        {
            foreach (var patch in prefixes.Concat(postfixes))
            {
                FileLog.Log($"[{patch.patchType}] {patch.patchMethod.FullName}");
                foreach (var parameter in patch.parameters)
                    FileLog.Log(
                        $"Name={parameter.Parameter.Name} BindingType={parameter.BindingType} Scope={parameter.Scope} Index={parameter.Index} Field{parameter.Field?.Name}");
            }
        }

        private void EmitParameterValue(ParameterBinding parameter)
        {
            Type parameterType = parameter.Parameter.ParameterType;

            switch (parameter.BindingType)
            {
                case BindingType.Parameter:
                case BindingType.Instance:
                {
                    EmitParameterLookup();
                    return;
                }

                case BindingType.Result:
                {
                    EmitResult(parameterType);
                    return;
                }

                case BindingType.ParameterField:
                case BindingType.InstanceField:
                {
                    EmitParameterLookup();

                    if (parameterType.IsByRef)
                        output.Add(new(OpCodes.Ldflda, parameter.Field));
                    else
                        output.Add(new(OpCodes.Ldfld, parameter.Field));

                    return;
                }

                case BindingType.State:
                {
                    if (parameterType.IsByRef)
                        output.Add(CodeInstruction.LoadLocalAddress(parameter.Index));
                    else
                        output.Add(CodeInstruction.LoadLocal(parameter.Index));

                    return;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            void EmitParameterLookup()
            {
                switch (parameter.Scope)
                {
                    case Scope.Outer: EmitCallerParameter(parameterType, parameter.Index); break;
                    case Scope.Inner: EmitTargetParameter(parameterType, parameter.Index); break;
                    default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
                }
            }
        }

        private void EmitResult(Type parameterType)
        {
            if (parameterType.IsByRef)
            {
                output.Add(CodeInstruction.LoadLocalAddress(resultLocalIndex));
            }
            else
                output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
        }

        private void EmitCallerParameter(Type type, int index)
        {
            if (type.IsByRef && !callerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldarga, index));
            else
                output.Add(CodeInstruction.LoadArgument(index));
            if (!type.IsByRef && callerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }

        private void EmitTargetParameter(Type type, int index)
        {
            if (type.IsByRef && !targetParameterTypes[index].IsByRef)
                output.Add(CodeInstruction.LoadLocalAddress(parameterToLocalIndex[index]));
            else
                output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[index]));
            if (!type.IsByRef && targetParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }

        private void EmitPrelude()
        {
            // Save all parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            for (int i = targetParameterTypes.Length - 1; i >= 0; i--)
            {
                parameterToLocalIndex[i] = AddLocal(targetParameterTypes[i]);
                output.Add(CodeInstruction.StoreLocal(parameterToLocalIndex[i]));
            }
        }

        private int AddLocal(Type type)
        {
            var localIndex = localTypes.Count;
            localTypes.Add(type);
            return localIndex;
        }

        private static Type[] GetParameterTypes(MemberInfo member)
        {
            return member switch
            {
                FieldInfo { IsStatic: true } => [],
                FieldInfo { IsStatic: false } field => [field.DeclaringType],
                MethodInfo { IsStatic: true } method => [.. method.GetParameters().Select(p => p.ParameterType)],
                MethodInfo { IsStatic: false } method => [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)],
                _ => throw new InvalidOperationException(),
            };
        }

        private static OpCode OpcodeFor(MemberInfo callee)
        {
            return callee switch
            {
                FieldInfo { IsStatic: true } => OpCodes.Ldsfld,
                FieldInfo { IsStatic: false } => OpCodes.Ldfld,
                MethodBase { IsVirtual: true } => OpCodes.Callvirt,
                MethodBase { IsVirtual: false } => OpCodes.Call,
                _ => throw new InvalidOperationException(),
            };
        }

        public InstructionMatcher.Rule BuildRule()
        {
            List<CodeInstruction> pattern =
            [
                new(OpcodeFor(target), target),
            ];

            EmitReplacement();

            return new InstructionMatcher.Rule
            {
                Min = 1,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.ToArray(),
                LocalTypes = localTypes.ToArray(),
                Name = $"{target.DeclaringType?.FullName}::{target.Name}",
            };
        }

        private readonly ILGenerator generator;
        private readonly MethodBase caller;
        private readonly MemberInfo target;
        private readonly MemberInfo? replacementTarget;
        private readonly List<PatchInfo> prefixes;
        private readonly List<PatchInfo> postfixes;
        private readonly List<CodeInstruction> output = [];

        private readonly List<Type> localTypes;

        private readonly bool debug;

        public RuleBuilder(
            ILGenerator generator,
            MethodBase caller,
            MemberInfo target,
            MethodInfo? replacementTarget,
            List<PatchInfo> prefixes,
            List<PatchInfo> postfixes,
            List<Type> localTypes)
        {
            this.generator = generator;
            this.caller = caller;
            this.target = target;
            this.replacementTarget = replacementTarget;
            this.prefixes = prefixes;
            this.postfixes = postfixes;
            this.localTypes = localTypes.ToList();

            debug = prefixes.Any(p => p.debug) || postfixes.Any(p => p.debug);

            targetType = target switch
            {
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                _ => throw new NotSupportedException(),
            };

            callerParameterTypes = GetParameterTypes(caller);
            targetParameterTypes = GetParameterTypes(target);

            parameterToLocalIndex = new int[targetParameterTypes.Length];
        }
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
                Min = 1,
                Max = 1,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = [],
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
        List<PatchInfo> patches = [];

        Dictionary<MethodInfo, StateBuilder<Type>> stateBuilders = new();

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

                    if (infixTargetAttribute == null)
                        continue;

                    MemberInfo? target = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                        infixTargetAttribute.parameterTypes, infixTargetAttribute.genericTypes);
                    if (target == null)
                        throw new InvalidOperationException("null wrapped member");

                    foreach (var infixPatchAttribute in infixPatchAttributes)
                    {
                        var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                        MethodInfo? caller = (MethodInfo?)GetMember(patchedType, infixPatchAttribute.methodName,
                            infixPatchAttribute.parameterTypes, infixPatchAttribute.genericTypes);
                        if (caller == null)
                            throw new InvalidOperationException("null target method");

                        if (!stateBuilders.TryGetValue(caller, out StateBuilder<Type> stateBuilder))
                            stateBuilder = stateBuilders[caller] = new();

                        var arguments = method.GetParameters().Select(param => BindParameter(param, caller, target, stateBuilder))
                            .ToArray();

                        patches.Add(new()
                        {
                            caller = caller,
                            target = target,
                            patchMethod = method,
                            patchType = infixTargetAttribute.patchType,
                            parameters = arguments,
                            debug = debug,
                        });
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error processing {type}:{method}", e);
                }
            }
        }

        AssemblyBuilder assemblyBuilder
            = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" },
                AssemblyBuilderAccess.RunAndSave);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

        foreach (IGrouping<MethodInfo, PatchInfo> patchGroup in patches.GroupBy(patch => patch.caller))
        {
            MethodInfo patchedMethod = patchGroup.Key;

            try
            {
                List<InstructionMatcher.Rule> rules = [];

                if (!stateBuilders.TryGetValue(patchedMethod, out var stateBuilder))
                    stateBuilder = new();

                if (stateBuilder.LocalTypes.Count > 0)
                    rules.Add(stateBuilder.BuildRule());

                foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patchGroup.GroupBy(patch => patch.target))
                {
                    var target = targetGroup.Key;
                    var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPrefix).ToList();
                    var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPostfix).ToList();

                    rules.Add(new()
                    {
                        LateGenerator = (_, _, generator) =>
                            RedirectRule_Core(generator, patchedMethod, target, null, prefixes, postfixes, stateBuilder.LocalTypes),
                    });
                }

                bool debug = patchGroup.Any(info => info.debug);

                var matcher = new InstructionMatcher
                {
                    Rules = rules,
                    LocalTypes = stateBuilder.LocalTypes,
                };

                MethodInfo transpiler = MakeTranspiler(moduleBuilder, matcher,
                    $"{patchedMethod.DeclaringType?.FullName?.Replace('.', '_')}_{patchedMethod.Name}_Transpiler", false);

                try
                {
                    harmony.Patch(patchedMethod, transpiler: new(transpiler) { debug = debug });
                }
                catch (Exception)
                {
                    // Rerun with debug on so we see what went wrong
                    InstructionMatcher.forceDebug = true;
                    harmony.Patch(patchedMethod, transpiler: new(transpiler) { debug = true });
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.DeclaringType}:{patchedMethod.Name}", e);
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
                        return new() { Parameter = parameter, BindingType = BindingType.InstanceField, Scope = Scope.Inner, Field = field };
                }

                // Look in target instance fields
                if (caller is { IsStatic: false })
                {
                    var field = caller.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.InstanceField, Scope = Scope.Outer, Field = field };
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
                                BindingType = BindingType.InstanceField,
                                Scope = Scope.Inner,
                                Index = closureIndex,
                                Field = field,
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

    private static MethodInfo MakeTranspiler(ModuleBuilder moduleBuilder, InstructionMatcher matcher, string typeName, bool debug)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

        FieldBuilder matcherField = typeBuilder.DefineField("matcher", typeof(InstructionMatcher),
            FieldAttributes.Public | FieldAttributes.Static);
        FieldBuilder debugField = typeBuilder.DefineField("debug", typeof(bool),
            FieldAttributes.Public | FieldAttributes.Static);

        MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
            typeof(List<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
        ILGenerator generator = methodBuilder.GetILGenerator();

        MethodInfo matchAndReplace
            = SymbolExtensions.GetMethodInfo(() => InstructionMatcher.MatchAndReplace((InstructionMatcher)null, null, null, null));

        generator.Emit(OpCodes.Ldsfld, matcherField);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldsfld, debugField);
        generator.Emit(OpCodes.Call, matchAndReplace);
        generator.Emit(OpCodes.Ret);

        Type type = typeBuilder.CreateType();
        type.GetField(matcherField.Name).SetValue(null, matcher);
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
