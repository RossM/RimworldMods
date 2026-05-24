using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TranspilerUtil
{
    public class MethodPatchWorker(ILGenerator generator, MethodBase caller, MemberInfo target, MemberInfo wrapper, List<MethodInfo> prefixes, List<MethodInfo> postfixes)
    {
        public ILGenerator generator = generator;
        public MethodBase caller = caller;
        public MemberInfo target = target;
        public MemberInfo wrapper = wrapper ?? target;
        public List<MethodInfo> prefixes = prefixes;
        public List<MethodInfo> postfixes = postfixes;
        public List<CodeInstruction> output = [];
        public List<Type> localTypes = [];

        private Type[] callerParameterTypes;
        private string[] callerParameterNames;
        private Type[] targetParameterTypes;
        private string[] targetParameterNames;
        private Type[] wrapperParameterTypes;
        private string[] wrapperParameterNames;
        private int firstNonMatchingParameter;
        private int[] parameterToLocalIndex;
        private int resultLocalIndex = -1;
        private Type targetType;

        public void EmitReplacement()
        {
            (callerParameterTypes, callerParameterNames) = GetParameterTypesAndNames(caller, "__caller");
            (targetParameterTypes, targetParameterNames) = GetParameterTypesAndNames(target, "__instance");
            (wrapperParameterTypes, wrapperParameterNames) = GetParameterTypesAndNames(wrapper, "__instance");

            EmitPrelude();

            targetType = target switch
            {
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                _ => throw new NotSupportedException(),
            };

            bool prefixUsesResult = prefixes.Any(method => method.GetParameters().Any(parameter => parameter.Name == "__result"));
            bool postfixUsesResult = postfixes.Any(method => method.GetParameters().Any(parameter => parameter.Name == "__result"));

            if (prefixUsesResult || postfixUsesResult)
            {
                resultLocalIndex = AddLocal(targetType);

                if (prefixUsesResult)
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

            // Match each parameter of the replacement method
            for (int i = firstNonMatchingParameter; i < wrapperParameterNames.Length; i++)
            {
                EmitParameterValue(wrapperParameterTypes[i], wrapperParameterNames[i]);
            }
            output.Add(new(OpcodeFor(wrapper), wrapper));

            if (skipLabel != null || postfixes.Count > 0)
            {
                if (resultLocalIndex >= 0)
                    output.Add(CodeInstruction.StoreLocal(resultLocalIndex));

                if (skipLabel is { } label)
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

            Debug.Log($"    {caller} -> {target}");
            foreach (var local in localTypes)
                Debug.Log($"        local {local}");
            foreach (var inst in output)
                Debug.Log($"        {inst}");
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
                output.Add(new(OpCodes.Ldloca, localIndex));
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
                if (targetParameterTypes[j].Name.StartsWith("<") &&
                    Attribute.IsDefined(targetParameterTypes[j], typeof(CompilerGeneratedAttribute)))
                {
                    var field = targetParameterTypes[j].GetField(parameterName, AccessTools.all);
                    if (field != null)
                    {
                        EmitTargetParameter(targetParameterTypes[j], j);
                        output.Add(new(OpCodes.Ldfld, field));
                        return;
                    }
                }
            }

            for (int j = 0; j < callerParameterTypes.Length; j++)
            {
                if (callerParameterTypes[j].Name.StartsWith("<") &&
                    Attribute.IsDefined(callerParameterTypes[j], typeof(CompilerGeneratedAttribute)))
                {
                    var field = callerParameterTypes[j].GetField(parameterName, AccessTools.all);
                    if (field != null)
                    {
                        EmitCallerParameter(callerParameterTypes[j], j);
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
                output.Add(new(OpCodes.Ldloca, resultLocalIndex));
            else
                output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
        }

        private void EmitCallerParameter(Type parameterType, int callerIndex)
        {
            if (parameterType.IsByRef && !callerParameterTypes[callerIndex].IsByRef)
                output.Add(new(OpCodes.Ldarga, callerIndex));
            else
                output.Add(CodeInstruction.LoadArgument(callerIndex));
        }

        private void EmitTargetParameter(Type parameterType, int targetIndex)
        {
            if (targetIndex < firstNonMatchingParameter)
                throw new InvalidOperationException(
                    $"Can't reuse parameter named '{targetParameterNames[targetIndex]}' of type {parameterType.FullName}");

            if (parameterType.IsByRef && !targetParameterTypes[targetIndex].IsByRef)
                output.Add(new(OpCodes.Ldloca, parameterToLocalIndex[targetIndex]));
            else
                output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[targetIndex]));
        }

        private void EmitPrelude()
        {
            firstNonMatchingParameter = 0;

            if (prefixes.Count == 0 && postfixes.Count == 0)
            {
                // Instructions which are already on the stack in the right order don't need to be saved and restored
                while (firstNonMatchingParameter < wrapperParameterNames.Length &&
                       firstNonMatchingParameter < targetParameterNames.Length &&
                       wrapperParameterNames[firstNonMatchingParameter] == targetParameterNames[firstNonMatchingParameter])
                {
                    firstNonMatchingParameter++;
                }

                if (firstNonMatchingParameter > 0)
                    Debug.Log($"    firstNonMatchingParameter={firstNonMatchingParameter}");
            }

            // Save all remaining parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            parameterToLocalIndex = new int[targetParameterTypes.Length];
            for (int i = targetParameterTypes.Length - 1; i >= firstNonMatchingParameter; i--)
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

    public static class Patcher
    {
        public enum PatchType
        {
            Wrapper,
            Prefix,
            Postfix,
        }

        private struct PatchInfo
        {
            public MemberInfo target;
            public MethodInfo caller;
            public MethodInfo patchMethod;
            public PatchType patchType;
        }

        public static MethodInfo MakeTranspiler(ModuleBuilder moduleBuilder, List<InstructionMatcher.Rule> rules, string typeName)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            FieldBuilder rulesField = typeBuilder.DefineField("rules", typeof(List<InstructionMatcher.Rule>),
                FieldAttributes.Public | FieldAttributes.Static);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                typeof(IEnumerable<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
            ILGenerator generator = methodBuilder.GetILGenerator();

            MethodInfo matchAndReplace = typeof(InstructionMatcher).GetMethod("MatchAndReplace",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(List<InstructionMatcher.Rule>), typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)],
                []);

            generator.Emit(OpCodes.Ldsfld, rulesField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Call, matchAndReplace);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            type.GetField(rulesField.Name).SetValue(null, rules);
            return type.GetMethod(methodBuilder.Name);
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
                            = (InfixTargetAttribute)Attribute.GetCustomAttribute(method, typeof(InfixWrapperAttribute)) ??
                              (InfixTargetAttribute)Attribute.GetCustomAttribute(method, typeof(InfixPrefixAttribute)) ??
                              (InfixTargetAttribute)Attribute.GetCustomAttribute(method, typeof(InfixPostfixAttribute));
                        var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(InfixPatchAttribute))
                            .Cast<InfixPatchAttribute>().ToArray();

                        if (infixTargetAttribute == null)
                            continue;

                        MemberInfo target = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                            infixTargetAttribute.parameterTypes);
                        if (target == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo caller = (MethodInfo)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes);
                            if (caller == null)
                                throw new InvalidOperationException("null target method");

                            patches.Add(new()
                            {
                                caller = caller, target = target, patchMethod = method,
                                patchType = infixTargetAttribute.patchType
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error processing {type}:{method}", e);
                    }
                }
            }

            Debug.Log("Patcher");

            AssemblyBuilder assemblyBuilder
                = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" },
                    AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

            foreach (IGrouping<MethodInfo, PatchInfo> patchGroup in patches.GroupBy(patch => patch.caller))
            {
                var patchedMethod = patchGroup.Key;

                Debug.Log($"{patchedMethod}");

                List<InstructionMatcher.Rule> rules = [];
                foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patchGroup.GroupBy(patch => patch.target))
                {
                    var target = targetGroup.Key;
                    var wrapper = targetGroup.SingleOrDefault(patch => patch.patchType == PatchType.Wrapper).patchMethod;
                    var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.Prefix).Select(patch => patch.patchMethod).ToList();
                    var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.Postfix).Select(patch => patch.patchMethod).ToList();

                    Debug.Log($"    {target}: wrapper={wrapper != null} prefixes={prefixes.Count} postfixes={postfixes.Count}");

                    rules.Add(new()
                    {
                        LateGenerator = (caller, _, generator) => 
                            RedirectRule_Core(generator,
                                patchedMethod,
                                target,
                                wrapper,
                                prefixes,
                                postfixes,
                                1)
                    });
                }

                MethodInfo transpiler = MakeTranspiler(moduleBuilder, rules,
                    $"{patchedMethod.DeclaringType?.FullName?.Replace('.', '_')}_{patchedMethod.Name}_Transpiler");

                harmony.Patch(patchedMethod, transpiler: new(transpiler));
            }
        }

        private static MemberInfo GetMember(Type type, string memberName, Type[] parameterTypes)
        {
            string[] nameParts = memberName.Split([':']);
            for (int i = 0; i < nameParts.Length - 1; i++)
                type = AccessTools.InnerTypes(type).First(type => type.Name.Contains(nameParts[i]));
            memberName = nameParts[nameParts.Length - 1];

            MemberInfo wrappedMember = parameterTypes == null
                ? type.GetMember(memberName, AccessTools.all).Single()
                : type.GetMethod(memberName, AccessTools.all, null,
                    parameterTypes, []);

            if (wrappedMember is PropertyInfo propertyInfo)
                wrappedMember = propertyInfo.GetMethod;
            return wrappedMember;
        }

        /// <summary>
        ///     This creates a rule that replaces all calls of a given method with calls of a given other method. The
        ///     new method's parameters will be filled with the values of the old method's parameters that have the
        ///     same name. If the old method doesn't have a parameter with that name, the parameters of the method
        ///     containing the call being modified are checked, and used if they match.
        ///     You can also use __instance to match the instance the method was invoked on, and __caller to match
        ///     the instance the calling method was invoked on.
        ///     If there isn't a parameter with a matching name, this will fall back to trying to match based
        ///     on parameter type, but this may result in less optimal code generation, and will give a warning.
        /// </summary>
        /// <param name="oldMember"></param>
        /// <param name="newMember"></param>
        /// <param name="minMatches"></param>
        /// <returns></returns>
        public static InstructionMatcher.Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember, int minMatches = 1)
        {
            return new()
            {
                LateGenerator = (caller, _, generator) => RedirectRule_Core(generator, caller, oldMember, newMember, [], [], minMatches)
            };
        }

        private static InstructionMatcher.Rule RedirectRule_Core(
            ILGenerator generator,
            MethodBase caller,
            MemberInfo target,
            MemberInfo wrapper,
            List<MethodInfo> prefixes,
            List<MethodInfo> postfixes,
            int minMatches)
        {
            List<CodeInstruction> pattern =
            [
                new(MethodPatchWorker.OpcodeFor(target), target),
            ];

            var methodPatchWorker = new MethodPatchWorker(generator, caller, target, wrapper, prefixes, postfixes);
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
