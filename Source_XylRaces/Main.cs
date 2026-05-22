using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using TranspilerUtil;
using UnityEngine;
using Verse;
using Exception = System.Exception;

namespace XylXenos
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main : Mod
    {
        struct PatchInfo
        {
            public MemberInfo wrappedMember;
            public MethodInfo targetMethod;
            public MethodInfo wrapper;
        }

        // ReSharper disable PossibleNullReferenceException
        private static Assembly MyAssembly => MethodBase.GetCurrentMethod().ReflectedType.Assembly;
        // ReSharper restore PossibleNullReferenceException

        static Main()
        {
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

            harmony.PatchAll();

            PatchInfix(harmony);
        }

        public Main(ModContentPack content) : base(content)
        {
            Settings.instance = GetSettings<Settings>();

            CodingStyleChecks();
        }

        private static void PatchInfix(Harmony harmony)
        {
            Assembly assembly = MyAssembly;

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

            //foreach (var patch in patches)
            //{
            //    Debug.Log($"InfixPatch: {patch.targetMethod} : {patch.wrappedMember} : {patch.wrapper}");
            //}

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

                Debug.Log($"Infix patching {group.Key.DeclaringType}::{group.Key} [{group.Count()} rule(s)]");

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

        private static MethodInfo MakeTranspiler(ModuleBuilder moduleBuilder, List<InstructionMatcher.Rule> rules, string typeName)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            FieldBuilder rulesField = typeBuilder.DefineField("rules", typeof(List<InstructionMatcher.Rule>),
                FieldAttributes.Public | FieldAttributes.Static);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                typeof(IEnumerable<CodeInstruction>), [typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator), typeof(MethodBase)]);
            ILGenerator generator = methodBuilder.GetILGenerator();

            Delegate matchAndReplace = MatchAndReplace;

            generator.Emit(OpCodes.Ldsfld, rulesField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Call, matchAndReplace.Method);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            type.GetField(rulesField.Name).SetValue(null, rules);
            return type.GetMethod(methodBuilder.Name);
        }

        public static List<CodeInstruction> MatchAndReplace(
            List<InstructionMatcher.Rule> rules,
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher() { Rules = rules }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        private static void CodingStyleChecks()
        {
            Assembly assembly = MyAssembly;
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                if (!Attribute.IsDefined(type, typeof(HarmonyPatch)))
                    continue;

                foreach (MethodInfo method in type.DeclaredMethods)
                {
                    var hasFeature = method.HasAttribute<FeatureAttribute>();
                    var hasPrefix = method.HasAttribute<HarmonyPrefix>();
                    var hasPostfix = method.HasAttribute<HarmonyPostfix>();
                    var hasTranspiler = method.HasAttribute<HarmonyTranspiler>();
                    var hasInfix = method.HasAttribute<InfixPatchAttribute>();
                    var hasWrappedMember = method.HasAttribute<WrappedMemberAttribute>();

                    if ((hasPrefix || hasPostfix || hasTranspiler || hasInfix) && !hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} is missing a [Feature] attribute");
                    if (!(hasPrefix || hasPostfix || hasTranspiler || hasInfix) && hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} has [Feature] but no Harmony attribute");

                    if (hasInfix != hasWrappedMember)
                        Log.Warning($"{type.Name}::{method.Name} has should have both [WrappedMember] and [InfixPatch]");

                    if (hasPrefix && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Prefix");
                    if (hasPostfix && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Postfix");
                    if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Transpiler");
                    if (hasWrappedMember && !method.Name.EndsWith("_Wrapper"))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Wrapper");
                }
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.instance.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
    }
}
