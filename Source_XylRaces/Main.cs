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
        // ReSharper disable PossibleNullReferenceException
        private static Assembly MyAssembly => MethodBase.GetCurrentMethod().ReflectedType.Assembly;
        // ReSharper restore PossibleNullReferenceException

        static Main()
        {
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

            harmony.PatchAll();

            Patcher.PatchInfix(harmony, MyAssembly);
        }

        public Main(ModContentPack content) : base(content)
        {
            Settings.instance = GetSettings<Settings>();

            CodingStyleChecks();
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
                    var hasInfixPatch = method.HasAttribute<InfixPatchAttribute>();
                    var hasInfixWrapper = method.HasAttribute<InfixWrapperAttribute>();
                    var hasInfixPrefix = method.HasAttribute<InfixPrefixAttribute>();
                    var hasInfixPostfix = method.HasAttribute<InfixPostfixAttribute>();

                    if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} is missing a [Feature] attribute");
                    if (!(hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} has [Feature] but no Harmony attribute");

                    if (hasInfixPatch != (hasInfixWrapper || hasInfixPrefix || hasInfixPostfix))
                        Log.Warning($"{type.Name}::{method.Name} has should have both [InfixPatch] and one of [InfixWrapper], [InfixPrefix] or [InfixPostfix]");

                    if ((hasPrefix || hasInfixPrefix) && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Prefix");
                    if ((hasPostfix || hasInfixPostfix) && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Postfix");
                    if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Transpiler");
                    if (hasInfixWrapper && !method.Name.EndsWith("_Wrapper"))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Wrapper");

                    var parameters = method.GetParameters();
                    ParameterInfo resultParameter = parameters.SingleOrDefault(p => p.Name == "__result");
                    if (hasPrefix || hasInfixPrefix)
                    {
                        if (resultParameter?.IsOut == false)
                            Log.Warning($"{type.Name}::{method.Name} should use 'out' for __result");
                        if (method.ReturnType.IsVoid() && resultParameter != null)
                            Log.Warning($"{type.Name}::{method.Name} returns void but uses __result");
                    }
                    if (hasPostfix || hasInfixPostfix)
                    {
                        if (resultParameter is { ParameterType.IsByRef: false })
                            Log.Warning($"{type.Name}::{method.Name} has a non-ref __result");
                    }
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
