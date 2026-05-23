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
    public static class Patcher
    {
        private struct PatchInfo
        {
            public MemberInfo wrappedMember;
            public MethodInfo targetMethod;
            public MethodInfo wrapper;
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
                        var wrappedMemberAttribute
                            = (InfixWrapperAttribute)Attribute.GetCustomAttribute(method, typeof(InfixWrapperAttribute));
                        var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(InfixPatchAttribute))
                            .Cast<InfixPatchAttribute>().ToArray();

                        if (wrappedMemberAttribute == null)
                            continue;

                        MemberInfo wrappedMember = GetMember(wrappedMemberAttribute.type, wrappedMemberAttribute.memberName,
                            wrappedMemberAttribute.parameterTypes);
                        if (wrappedMember == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo targetMethod = (MethodInfo)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes);
                            if (targetMethod == null)
                                throw new InvalidOperationException("null target method");

                            patches.Add(new() { targetMethod = targetMethod, wrappedMember = wrappedMember, wrapper = method });
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

            foreach (IGrouping<MethodInfo, PatchInfo> group in patches.GroupBy(patch => patch.targetMethod))
            {
                List<InstructionMatcher.Rule> rules = [];
                foreach (var patch in group)
                    rules.Add(MakeRedirectRule(patch.wrappedMember, patch.wrapper));

                MethodInfo transpiler = MakeTranspiler(moduleBuilder, rules,
                    $"{group.Key.DeclaringType?.FullName?.Replace('.', '_')}_{group.Key.Name}_Transpiler");

                //Debug.Log($"Infix patching {group.Key.DeclaringType}::{group.Key}");
                //foreach (var patch in group)
                //    Debug.Log($"  {patch.wrappedMember} -> {patch.wrapper}");

                harmony.Patch(group.Key, transpiler: new HarmonyMethod(transpiler));
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
            MemberInfo replacement,
            int minMatches)
        {
            (Type[] callerParameterTypes, string[] callerParameterNames) = GetParameterTypesAndNames(caller, "__caller");
            (Type[] calleeParameterTypes, string[] calleeParameterNames) = GetParameterTypesAndNames(callee, "__instance");
            (Type[] replacementParameterTypes, string[] replacementParameterNames) = GetParameterTypesAndNames(replacement, "__instance");

            List<CodeInstruction> pattern = [];
            List<CodeInstruction> output = [];
            List<Type> localTypes = [];

            pattern.Add(new CodeInstruction(OpcodeFor(callee), callee));

            // Instructions which are already on the stack in the right order don't need to be saved and restored
            int firstNonMatchingParameter = 0;
            while (firstNonMatchingParameter < replacementParameterNames.Length &&
                   firstNonMatchingParameter < calleeParameterNames.Length &&
                   replacementParameterNames[firstNonMatchingParameter] == calleeParameterNames[firstNonMatchingParameter])
            {
                firstNonMatchingParameter++;
            }

            // Save all remaining parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            int[] parameterToLocalIndex = new int[calleeParameterTypes.Length];
            for (int i = calleeParameterTypes.Length - 1; i >= firstNonMatchingParameter; i--)
            {
                parameterToLocalIndex[i] = localTypes.Count;
                localTypes.Add(calleeParameterTypes[i]);
                output.Add(CodeInstruction.StoreLocal(parameterToLocalIndex[i]));
            }

            // Match each parameter of the replacement method
            for (int i = firstNonMatchingParameter; i < replacementParameterNames.Length; i++)
            {
                string replacementParameterName = replacementParameterNames[i];
                Type replacementParameterType = replacementParameterTypes[i];

                int calleeIndex = calleeParameterNames.FirstIndexOf(name => name == replacementParameterName);
                if (calleeIndex >= 0)
                {
                    if (calleeIndex < firstNonMatchingParameter)
                        throw new InvalidOperationException(
                            $"Can't reuse parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
                    output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[calleeIndex]));
                    continue;
                }

                int callerIndex = callerParameterNames.FirstIndexOf(name => name == replacementParameterName);
                if (callerIndex >= 0)
                {
                    output.Add(CodeInstruction.LoadArgument(callerIndex));
                    continue;
                }

                bool found = false;
                for (int j = 0; j < calleeParameterTypes.Length; j++)
                {
                    if (calleeParameterTypes[j].Name.StartsWith("<") &&
                        Attribute.IsDefined(calleeParameterTypes[j], typeof(CompilerGeneratedAttribute)))
                    {
                        var field = calleeParameterTypes[j].GetField(replacementParameterName, AccessTools.all);
                        if (field != null)
                        {
                            output.Add(CodeInstruction.LoadArgument(j));
                            output.Add(new CodeInstruction(OpCodes.Ldfld, field));
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    continue;

                for (int j = 0; j < callerParameterTypes.Length; j++)
                {
                    if (callerParameterTypes[j].Name.StartsWith("<") &&
                        Attribute.IsDefined(callerParameterTypes[j], typeof(CompilerGeneratedAttribute)))
                    {
                        var field = callerParameterTypes[j].GetField(replacementParameterName, AccessTools.all);
                        if (field != null)
                        {
                            output.Add(CodeInstruction.LoadArgument(j));
                            output.Add(new CodeInstruction(OpCodes.Ldfld, field));
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    continue;

                calleeIndex = calleeParameterTypes.FirstIndexOf(type => type == replacementParameterType);
                if (calleeIndex >= 0)
                {
                    Log.Warning(
                        $"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({callee.Name} -> {replacement.Name}): Matching by type: {replacementParameterType.Name} {replacementParameterName} = {calleeParameterTypes[calleeIndex].Name} {calleeParameterNames[calleeIndex]}");
                    if (calleeIndex < firstNonMatchingParameter)
                        throw new InvalidOperationException(
                            $"Can't reuse parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
                    output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[calleeIndex]));
                    continue;
                }

                callerIndex = callerParameterTypes.FirstIndexOf(type => type == replacementParameterType);
                if (callerIndex >= 0)
                {
                    Log.Warning(
                        $"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({callee.Name} -> {replacement.Name}): Matching by type: {replacementParameterType.Name} {replacementParameterName} = caller's {callerParameterTypes[callerIndex].Name} {callerParameterNames[callerIndex]}");
                    output.Add(CodeInstruction.LoadArgument(callerIndex));
                    continue;
                }

                throw new InvalidOperationException(
                    $"Couldn't find parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
            }

            output.Add(new CodeInstruction(OpcodeFor(replacement), replacement));

            var rule = new InstructionMatcher.Rule()
            {
                Min = minMatches,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.ToArray(),
                LocalTypes = localTypes.ToArray(),
            };

            return rule;
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

        private static OpCode OpcodeFor(MemberInfo callee)
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
}
