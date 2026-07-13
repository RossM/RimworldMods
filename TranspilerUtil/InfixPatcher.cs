using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace TranspilerUtil;

public static class InfixPatcher
{
    public enum PatchType
    {
        Prefix,
        Postfix,
    }

    private enum Scope
    {
        Inner,
        Outer,
    }

    private enum BindingType
    {
        Parameter,
        Instance,
        Result,
        ParameterField,
        InstanceField,
    }

    private struct ParameterBinding
    {
        public ParameterInfo Parameter;
        public Scope Scope;
        public BindingType BindingType;
        public int Index;
        public FieldInfo? Field;
    }

    private delegate List<CodeInstruction> MatchAndReplaceFn(
        List<InstructionMatcher.Rule> rules,
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        bool debug);

    private class MethodPatchWorker
    {
        private readonly Type[] callerParameterTypes;
        private readonly Type[] targetParameterTypes;
        private readonly int[] parameterToLocalIndex;
        private int resultLocalIndex = -1;
        private readonly Type targetType;

        public void EmitReplacement()
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
                        EmitInitialization(targetType, resultLocalIndex);
                }
            }

            Label? skipLabel = null;
            foreach (var prefix in prefixes)
            {
                MethodInfo patchMethod = prefix.patchMethod;
                foreach (var parameter in prefix.parameters)
                    EmitParameterValue(parameter);

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
            foreach (var prefix in prefixes)
            {
                Debug.Log($"prefix {prefix.patchMethod.DeclaringType?.FullName}::{prefix.patchMethod.Name}");
                foreach (var parameter in prefix.parameters)
                    Debug.Log(
                        $"Name={parameter.Parameter.Name} BindingType={parameter.BindingType} Scope={parameter.Scope} Index={parameter.Index} Field{parameter.Field?.Name}");
            }

            foreach (var postfix in postfixes)
            {
                Debug.Log($"postfix {postfix.patchMethod.DeclaringType?.FullName}::{postfix.patchMethod.Name}");
                foreach (var parameter in postfix.parameters)
                    Debug.Log(
                        $"Name={parameter.Parameter.Name} BindingType={parameter.BindingType} Scope={parameter.Scope} Index={parameter.Index} Field{parameter.Field?.Name}");
            }
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

        // ReSharper disable MemberCanBePrivate.Local
        public readonly ILGenerator generator;
        public readonly MethodBase caller;
        public readonly MemberInfo target;
        public readonly MemberInfo? replacementTarget;
        public readonly List<PatchInfo> prefixes;
        public readonly List<PatchInfo> postfixes;
        public readonly List<CodeInstruction> output = [];

        public readonly List<Type> localTypes = [];

        public readonly bool debug;

        public MethodPatchWorker(
            ILGenerator generator,
            MethodBase caller,
            MemberInfo target,
            MethodInfo? replacementTarget,
            List<PatchInfo> prefixes,
            List<PatchInfo> postfixes)
        {
            this.generator = generator;
            this.caller = caller;
            this.target = target;
            this.replacementTarget = replacementTarget;
            this.prefixes = prefixes;
            this.postfixes = postfixes;

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
        // ReSharper restore MemberCanBePrivate.Local
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

    public static void PatchInfix(Harmony harmony, Assembly assembly)
    {
        List<PatchInfo> patches = [];

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
                        = (InfixTargetAttribute?)Attribute.GetCustomAttribute(method, typeof(InfixPrefixAttribute)) ??
                          (InfixTargetAttribute?)Attribute.GetCustomAttribute(method, typeof(InfixPostfixAttribute));
                    var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(InfixPatchAttribute))
                        .Cast<InfixPatchAttribute>().ToArray();
                    bool debug = Attribute.GetCustomAttribute(method, typeof(InfixDebugAttribute)) != null;

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

                        var arguments = method.GetParameters().Select(param => BindParameter(param, caller, target)).ToArray();

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
                foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patchGroup.GroupBy(patch => patch.target))
                {
                    var target = targetGroup.Key;
                    var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.Prefix).ToList();
                    var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.Postfix).ToList();

                    rules.Add(new()
                    {
                        LateGenerator = (_, _, generator) =>
                            RedirectRule_Core(generator,
                                patchedMethod,
                                target,
                                null,
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

    private static ParameterBinding BindParameter(ParameterInfo parameter, MethodInfo caller, MemberInfo target)
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
            LateGenerator = (caller, _, generator) => RedirectRule_Core(generator, caller, oldMember, newMember, [], [], minMatches)
        };
    }

    private static InstructionMatcher.Rule RedirectRule_Core(
        ILGenerator generator,
        MethodBase caller,
        MemberInfo target,
        MethodInfo replacementTarget,
        List<PatchInfo> prefixes,
        List<PatchInfo> postfixes,
        int minMatches)
    {
        List<CodeInstruction> pattern =
        [
            new(MethodPatchWorker.OpcodeFor(target), target),
        ];

        var methodPatchWorker = new MethodPatchWorker(generator, caller, target, replacementTarget, prefixes, postfixes);
        methodPatchWorker.EmitReplacement();

        var rule = new InstructionMatcher.Rule
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
