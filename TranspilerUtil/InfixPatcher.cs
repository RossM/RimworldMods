using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;

namespace TranspilerUtil
{
    public static class InfixPatcher
    {
        public enum PatchType
        {
            Prefix,
            Postfix,
        }

        delegate List<CodeInstruction> MatchAndReplaceFn(
            List<InstructionMatcher.Rule> rules,
            MethodBase method,
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            bool debug);

        private class MethodPatchWorker(
            ILGenerator generator,
            MethodBase caller,
            MemberInfo target,
            List<MethodInfo> prefixes,
            List<MethodInfo> postfixes)
        {
            public readonly ILGenerator generator = generator;
            public readonly MethodBase caller = caller;
            public readonly MemberInfo target = target;
            public readonly List<MethodInfo> prefixes = prefixes;
            public readonly List<MethodInfo> postfixes = postfixes;
            public readonly List<CodeInstruction> output = [];
            public readonly List<Type> localTypes = [];

            private Type[] callerParameterTypes;
            private string[] callerParameterNames;
            private Type[] targetParameterTypes;
            private string[] targetParameterNames;
            private int[] parameterToLocalIndex;
            private int resultLocalIndex = -1;
            private Type targetType;

            public void EmitReplacement()
            {
                (callerParameterTypes, callerParameterNames) = GetParameterTypesAndNames(caller, "__caller");
                (targetParameterTypes, targetParameterNames) = GetParameterTypesAndNames(target, "__instance");

                EmitPrelude();

                targetType = target switch
                {
                    FieldInfo field => field.FieldType,
                    MethodInfo method => method.ReturnType,
                    _ => throw new NotSupportedException(),
                };

                var prefixesUsingResult = prefixes.Where(method => method.GetParameters().Any(parameter => parameter.Name == "__result"))
                    .ToList();
                var postfixesUsingResult = postfixes.Where(method => method.GetParameters().Any(parameter => parameter.Name == "__result"))
                    .ToList();

                if (prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
                {
                    resultLocalIndex = AddLocal(targetType);

                    if (prefixesUsingResult.Count > 0 &&
                        !prefixesUsingResult[0].GetParameters().Single(parameter => parameter.Name == "__result").IsOut)
                        EmitInitialization(targetType, resultLocalIndex);
                }

                Label? skipLabel = null;
                foreach (var prefix in prefixes)
                {
                    (Type[] types, string[] names) = GetParameterTypesAndNames(prefix, null);
                    for (int i = 0; i < types.Length; i++)
                    {
                        EmitParameterValue(types[i], names[i]);
                    }

                    output.Add(new(OpcodeFor(prefix), prefix));
                    if (!prefix.ReturnType.IsVoid())
                    {
                        output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
                    }
                }

                for (int i = 0; i < targetParameterTypes.Length; i++)
                {
                    EmitTargetParameter(targetParameterTypes[i], i);
                }

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
                        (Type[] types, string[] names) = GetParameterTypesAndNames(postfix, null);
                        for (int i = 0; i < types.Length; i++)
                        {
                            EmitParameterValue(types[i], names[i]);
                        }

                        output.Add(new(OpcodeFor(postfix), postfix));
                        if (!postfix.ReturnType.IsVoid())
                            output.Add(new(OpCodes.Pop));
                    }

                    if (resultLocalIndex >= 0)
                    {
                        output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
                    }
                }

                //Debug.Log($"    {caller} -> {target}");
                //foreach (var local in localTypes)
                //    Debug.Log($"        local {local}");
                //foreach (var inst in output)
                //    Debug.Log($"        {inst}");
            }

            private void EmitInitialization(Type type, int localIndex)
            {
                if (type.IsByRef)
                    throw new NotImplementedException($"IsByRef targetType {type}");

                if (type.IsClass)
                {
                    output.Add(new(OpCodes.Ldnull));
                    output.Add(CodeInstruction.StoreLocal(localIndex));
                }
                else if (type.IsStruct())
                {
                    output.Add(CodeInstructionUtil.LoadLocalAddress(localIndex));
                    output.Add(new(OpCodes.Initobj, type));
                }
                else if (type.IsValueType)
                {
                    if (type == typeof(float))
                        output.Add(new(OpCodes.Ldc_R4, (float)0));
                    else if (type == typeof(double))
                        output.Add(new(OpCodes.Ldc_R8, (double)0));
                    else if (type == typeof(long) || type == typeof(ulong))
                        output.Add(new(OpCodes.Ldc_I8, (long)0));
                    else
                        output.Add(new(OpCodes.Ldc_I4_0));

                    output.Add(CodeInstruction.StoreLocal(localIndex));
                }
                else
                    throw new NotImplementedException($"targetType {type}");
            }

            private void EmitParameterValue(Type parameterType, string parameterName)
            {
                if (parameterName == "__result" && resultLocalIndex >= 0)
                {
                    EmitResult(parameterType);
                    return;
                }

                if (parameterName.StartsWith("___"))
                {
                    string fieldName = parameterName[3 ..];

                    if (target is FieldInfo { IsStatic: false } or MethodInfo { IsStatic: false })
                    {
                        var field = target.DeclaringType!.GetField(fieldName, AccessTools.all);
                        if (field is { IsStatic: false })
                        {
                            EmitTargetParameter(target.DeclaringType, 0);
                            if (parameterType.IsByRef)
                                output.Add(new(OpCodes.Ldflda, field));
                            else
                                output.Add(new(OpCodes.Ldfld, field));
                            return;
                        }
                    }

                    if (caller is MethodInfo { IsStatic: false })
                    {
                        var field = caller.DeclaringType!.GetField(fieldName, AccessTools.all);
                        if (field is { IsStatic: false })
                        {
                            EmitCallerParameter(caller.DeclaringType, 0);
                            if (parameterType.IsByRef)
                                output.Add(new(OpCodes.Ldflda, field));
                            else
                                output.Add(new(OpCodes.Ldfld, field));
                            return;
                        }
                    }
                }

                int targetIndex = targetParameterNames.FirstIndexOf(name => name == parameterName);
                if (targetIndex >= 0)
                {
                    EmitTargetParameter(parameterType, targetIndex);
                    return;
                }

                int callerIndex = callerParameterNames.FirstIndexOf(name => name == parameterName);
                if (callerIndex >= 0)
                {
                    EmitCallerParameter(parameterType, callerIndex);
                    return;
                }

                for (int j = 0; j < targetParameterTypes.Length; j++)
                {
                    Type targetParameterType = targetParameterTypes[j];
                    Type type = targetParameterType.IsByRef ? targetParameterType.GetElementType() : targetParameterType;
                    if (type!.Name.StartsWith("<") &&
                        Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)))
                    {
                        var field = type.GetField(parameterName, AccessTools.all);
                        if (field != null)
                        {
                            EmitTargetParameter(targetParameterType, j);
                            if (parameterType.IsByRef)
                                output.Add(new(OpCodes.Ldflda, field));
                            else
                                output.Add(new(OpCodes.Ldfld, field));
                            return;
                        }
                    }
                }

                throw new InvalidOperationException(
                    $"Couldn't find parameter named '{parameterName}' of type {parameterType.FullName}");
            }

            private void EmitResult(Type parameterType)
            {
                if (parameterType.IsByRef)
                {
                    output.Add(CodeInstructionUtil.LoadLocalAddress(resultLocalIndex));
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
                    output.Add(CodeInstructionUtil.LoadLocalAddress(parameterToLocalIndex[index]));
                else
                    output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[index]));
                if (!type.IsByRef && targetParameterTypes[index].IsByRef)
                    output.Add(new(OpCodes.Ldobj, type));
            }

            private void EmitPrelude()
            {
                // Save all parameters to local. The matcher will handle renumbering the locals to new
                // unused local indexes.
                parameterToLocalIndex = new int[targetParameterTypes.Length];
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

            private static (Type[] types, string[] names) GetParameterTypesAndNames(MemberInfo member, string instanceName)
            {
                return member switch
                {
                    FieldInfo { IsStatic: true } => (
                        [],
                        []),
                    FieldInfo field => (
                        [field.DeclaringType],
                        [instanceName]),
                    MethodInfo { IsStatic: true } method => (
                        [.. (method.GetParameters()).Select(p => p.ParameterType)],
                        [.. (method.GetParameters()).Select(p => p.Name)]),
                    MethodInfo method => (
                        [method.DeclaringType, .. (method.GetParameters()).Select(p => p.ParameterType)],
                        [instanceName, .. (method.GetParameters()).Select(p => p.Name)]),
                    _ => throw new InvalidOperationException()
                };
            }

            public static OpCode OpcodeFor(MemberInfo callee)
            {
                return callee switch
                {
                    FieldInfo { IsStatic: true } => OpCodes.Ldsfld,
                    FieldInfo => OpCodes.Ldfld,
                    MethodBase { IsVirtual: true } => OpCodes.Callvirt,
                    MethodBase => OpCodes.Call,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        private struct PatchInfo
        {
            public MemberInfo target;
            public MethodInfo caller;
            public MethodInfo patchMethod;
            public PatchType patchType;
            public bool debug;
        }

        public static void PatchInfix(Harmony harmony, Assembly assembly)
        {
            List<PatchInfo> patches = [];

            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                var harmonyAttribute = (HarmonyPatch)Attribute.GetCustomAttribute(type, typeof(HarmonyPatch));
                if (harmonyAttribute == null)
                    continue;

                foreach (MethodInfo method in type.DeclaredMethods)
                {
                    try
                    {
                        var infixTargetAttribute
                            = (InfixTargetAttribute)Attribute.GetCustomAttribute(method, typeof(InfixPrefixAttribute)) ??
                              (InfixTargetAttribute)Attribute.GetCustomAttribute(method, typeof(InfixPostfixAttribute));
                        var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(InfixPatchAttribute))
                            .Cast<InfixPatchAttribute>().ToArray();
                        bool debug = Attribute.GetCustomAttribute(method, typeof(InfixDebugAttribute)) != null;

                        if (infixTargetAttribute == null)
                            continue;

                        MemberInfo target = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                            infixTargetAttribute.parameterTypes, infixTargetAttribute.genericTypes);
                        if (target == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo caller = (MethodInfo)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes, infixPatchAttribute.genericTypes);
                            if (caller == null)
                                throw new InvalidOperationException("null target method");

                            patches.Add(new()
                            {
                                caller = caller, target = target, patchMethod = method,
                                patchType = infixTargetAttribute.patchType, debug = debug,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error processing {type}:{method}", e);
                    }
                }
            }

            //Debug.Log("Patcher");

            AssemblyBuilder assemblyBuilder
                = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" },
                    AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

            foreach (IGrouping<MethodInfo, PatchInfo> patchGroup in patches.GroupBy(patch => patch.caller))
            {
                var patchedMethod = patchGroup.Key;

                try
                {
                    //Debug.Log($"{patchedMethod}");

                    List<InstructionMatcher.Rule> rules = [];
                    foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patchGroup.GroupBy(patch => patch.target))
                    {
                        var target = targetGroup.Key;
                        var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.Prefix).Select(patch => patch.patchMethod)
                            .ToList();
                        var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.Postfix).Select(patch => patch.patchMethod)
                            .ToList();

                        //Debug.Log($"    {target}: prefixes={prefixes.Count} postfixes={postfixes.Count}");

                        rules.Add(new()
                        {
                            LateGenerator = (caller, _, generator) =>
                                RedirectRule_Core(generator,
                                    patchedMethod,
                                    target,
                                    prefixes,
                                    postfixes,
                                    1)
                        });
                    }

                    bool debug = patchGroup.Any(info => info.debug);

                    MethodInfo transpiler = MakeTranspiler(moduleBuilder, rules,
                        $"{patchedMethod.DeclaringType?.FullName?.Replace('.', '_')}_{patchedMethod.Name}_Transpiler", debug);

                    try
                    {
                        harmony.Patch(patchedMethod, transpiler: new(transpiler));
                    }
                    catch (Exception)
                    {
                        // Rerun with debug on so we see what went wrong
                        InstructionMatcher.forceDebug = true;
                        harmony.Patch(patchedMethod, transpiler: new(transpiler));
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error patching {patchedMethod.DeclaringType}:{patchedMethod.Name}", e);
                }
            }
        }

        private static MemberInfo GetMember(Type type, string memberName, Type[] parameterTypes, Type[] genericTypes)
        {
            string[] nameParts = memberName.Split([':']);
            for (int i = 0; i < nameParts.Length - 1; i++)
                type = AccessTools.InnerTypes(type).First(type1 => type1.Name.Contains(nameParts[i]));
            memberName = nameParts[^1];

            MemberInfo wrappedMember;
            if (genericTypes != null)
            {
                wrappedMember = type
                    .GetMethods().Single(m => m.Name == memberName && m.IsGenericMethod && m.GetGenericArguments().Length == genericTypes.Length)
                    .MakeGenericMethod(genericTypes);
            }
            else if (parameterTypes != null)
            {
                wrappedMember = type.GetMethod(memberName, AccessTools.all, null,
                    parameterTypes, []);
            }
            else
            {
                wrappedMember = type.GetMember(memberName, AccessTools.all).Single();
            }

            if (wrappedMember is PropertyInfo propertyInfo)
                wrappedMember = propertyInfo.GetMethod;
            return wrappedMember;
        }

        private static MethodInfo MakeTranspiler(ModuleBuilder moduleBuilder, List<InstructionMatcher.Rule> rules, string typeName, bool debug)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            FieldBuilder rulesField = typeBuilder.DefineField("rules", typeof(List<InstructionMatcher.Rule>),
                FieldAttributes.Public | FieldAttributes.Static);
            FieldBuilder debugField = typeBuilder.DefineField("debug", typeof(bool),
                FieldAttributes.Public | FieldAttributes.Static);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                typeof(IEnumerable<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
            ILGenerator generator = methodBuilder.GetILGenerator();

            MethodInfo matchAndReplace = ((MatchAndReplaceFn)InstructionMatcher.MatchAndReplace).Method;

            generator.Emit(OpCodes.Ldsfld, rulesField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldsfld, debugField);
            generator.Emit(OpCodes.Call, matchAndReplace);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            type.GetField(rulesField.Name).SetValue(null, rules);
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
        public static InstructionMatcher.Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember, int minMatches = 1)
        {
            return new()
            {
                LateGenerator = (caller, _, generator) => RedirectRule_Core(generator, caller, oldMember, [], [], minMatches)
            };
        }

        private static InstructionMatcher.Rule RedirectRule_Core(
            ILGenerator generator,
            MethodBase caller,
            MemberInfo target,
            List<MethodInfo> prefixes,
            List<MethodInfo> postfixes,
            int minMatches)
        {
            List<CodeInstruction> pattern =
            [
                new(MethodPatchWorker.OpcodeFor(target), target),
            ];

            var methodPatchWorker = new MethodPatchWorker(generator, caller, target, prefixes, postfixes);
            methodPatchWorker.EmitReplacement();

            var rule = new InstructionMatcher.Rule()
            {
                Min = minMatches,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = methodPatchWorker.output.ToArray(),
                LocalTypes = methodPatchWorker.localTypes.ToArray(),
            };

            return rule;
        }
    }
}
