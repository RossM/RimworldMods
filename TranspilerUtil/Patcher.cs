using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace TranspilerUtil
{
    public class MethodPatchWorker(MethodBase caller, MemberInfo target, MemberInfo wrapper)
    {
        public MethodBase caller = caller;
        public MemberInfo target = target;
        public MemberInfo wrapper = wrapper;
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

        public void EmitReplacement()
        {
            (callerParameterTypes, callerParameterNames) = GetParameterTypesAndNames(caller, "__caller");
            (targetParameterTypes, targetParameterNames) = GetParameterTypesAndNames(target, "__instance");
            (wrapperParameterTypes, wrapperParameterNames) = GetParameterTypesAndNames(wrapper, "__instance");

            EmitPrelude();

            // Match each parameter of the replacement method
            for (int i = firstNonMatchingParameter; i < wrapperParameterNames.Length; i++)
            {
                EmitParameterValue(wrapperParameterTypes[i], wrapperParameterNames[i]);
            }

            output.Add(new CodeInstruction(OpcodeFor(wrapper), wrapper));
        }

        private void EmitParameterValue(Type parameterType, string parameterName)
        {
            int targetIndex = targetParameterNames.FirstIndexOf(name => name == parameterName);
            if (targetIndex >= 0)
            {
                if (targetIndex < firstNonMatchingParameter)
                    throw new InvalidOperationException(
                        $"Can't reuse parameter named '{parameterName}' of type {parameterType.FullName}");
                output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[targetIndex]));
                return;
            }

            int callerIndex = callerParameterNames.FirstIndexOf(name => name == parameterName);
            if (callerIndex >= 0)
            {
                output.Add(CodeInstruction.LoadArgument(callerIndex));
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
                        output.Add(CodeInstruction.LoadArgument(j));
                        output.Add(new CodeInstruction(OpCodes.Ldfld, field));
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
                        output.Add(CodeInstruction.LoadArgument(j));
                        output.Add(new CodeInstruction(OpCodes.Ldfld, field));
                        return;
                    }
                }
            }

            targetIndex = targetParameterTypes.FirstIndexOf(type => type == parameterType);
            if (targetIndex >= 0)
            {
                Log.Warning(
                    $"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({target.Name} -> {wrapper.Name}): Matching by type: {parameterType.Name} {parameterName} = {targetParameterTypes[targetIndex].Name} {targetParameterNames[targetIndex]}");
                if (targetIndex < firstNonMatchingParameter)
                    throw new InvalidOperationException(
                        $"Can't reuse parameter named '{parameterName}' of type {parameterType.FullName}");
                output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[targetIndex]));
                return;
            }

            callerIndex = callerParameterTypes.FirstIndexOf(type => type == parameterType);
            if (callerIndex >= 0)
            {
                Log.Warning(
                    $"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({target.Name} -> {wrapper.Name}): Matching by type: {parameterType.Name} {parameterName} = caller's {callerParameterTypes[callerIndex].Name} {callerParameterNames[callerIndex]}");
                output.Add(CodeInstruction.LoadArgument(callerIndex));
                return;
            }

            throw new InvalidOperationException(
                $"Couldn't find parameter named '{parameterName}' of type {parameterType.FullName}");
        }

        private void EmitPrelude()
        {
            // Instructions which are already on the stack in the right order don't need to be saved and restored
            firstNonMatchingParameter = 0;
            while (firstNonMatchingParameter < wrapperParameterNames.Length &&
                   firstNonMatchingParameter < targetParameterNames.Length &&
                   wrapperParameterNames[firstNonMatchingParameter] == targetParameterNames[firstNonMatchingParameter])
            {
                firstNonMatchingParameter++;
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
            public MemberInfo wrappedMember;
            public MethodInfo targetMethod;
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

                        MemberInfo wrappedMember = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                            infixTargetAttribute.parameterTypes);
                        if (wrappedMember == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo targetMethod = (MethodInfo)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes);
                            if (targetMethod == null)
                                throw new InvalidOperationException("null target method");

                            patches.Add(new()
                            {
                                targetMethod = targetMethod, wrappedMember = wrappedMember, patchMethod = method,
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

            AssemblyBuilder assemblyBuilder
                = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" },
                    AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

            foreach (IGrouping<MethodInfo, PatchInfo> targetGroup in patches.GroupBy(patch => patch.targetMethod))
            {
                var targetMethod = targetGroup.Key;
                List<InstructionMatcher.Rule> rules = [];
                foreach (IGrouping<MemberInfo, PatchInfo> wrapGroup in targetGroup.GroupBy(patch => patch.wrappedMember))
                {
                    var wrappedMember = wrapGroup.Key;
                    var wrapperMethod = wrapGroup.SingleOrDefault(patch => patch.patchType == PatchType.Wrapper).patchMethod;
                    var prefixMethods = wrapGroup.Where(patch => patch.patchType == PatchType.Prefix).Select(patch => patch.targetMethod)
                        .ToList();
                    var postfixMethods = wrapGroup.Where(patch => patch.patchType == PatchType.Postfix).Select(patch => patch.targetMethod)
                        .ToList();

                    if (prefixMethods.Count == 0 && postfixMethods.Count == 0)
                    {
                        rules.Add(MakeRedirectRule(wrappedMember, wrapperMethod));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                MethodInfo transpiler = MakeTranspiler(moduleBuilder, rules,
                    $"{targetMethod.DeclaringType?.FullName?.Replace('.', '_')}_{targetMethod.Name}_Transpiler");

                harmony.Patch(targetMethod, transpiler: new HarmonyMethod(transpiler));
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
                LateGenerator = (caller, _) => RedirectRule_Core(caller, oldMember, newMember, minMatches)
            };
        }

        private static InstructionMatcher.Rule RedirectRule_Core(
            MethodBase caller,
            MemberInfo callee,
            MemberInfo wrapper,
            int minMatches)
        {
            List<CodeInstruction> pattern =
            [
                new(MethodPatchWorker.OpcodeFor(callee), callee),
            ];

            var methodPatchWorker = new MethodPatchWorker(caller, callee, wrapper);
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
