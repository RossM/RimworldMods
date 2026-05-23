using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

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
                            = (WrappedMemberAttribute)Attribute.GetCustomAttribute(method, typeof(WrappedMemberAttribute));
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
                    rules.Add(InstructionMatcher.MakeRedirectRule(patch.wrappedMember, patch.wrapper));

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
    }
}
