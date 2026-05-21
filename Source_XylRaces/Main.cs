using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main : Mod
    {
        static Main()
        {
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");
            harmony.PatchAll();
        }

        public Main(ModContentPack content) : base(content)
        {
            Settings.instance = GetSettings<Settings>();

            CodingStyleChecks();
        }

        private static void CodingStyleChecks()
        {
            // ReSharper disable PossibleNullReferenceException
            var assembly = MethodBase.GetCurrentMethod().ReflectedType.Assembly;
            // ReSharper restore PossibleNullReferenceException
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                foreach (MethodInfo method in type.DeclaredMethods)
                {
                    var hasFeature = method.HasAttribute<FeatureAttribute>();
                    var hasPrefix = method.HasAttribute<HarmonyPrefix>();
                    var hasPostfix = method.HasAttribute<HarmonyPostfix>();
                    var hasTranspiler = method.HasAttribute<HarmonyTranspiler>();

                    if ((hasPrefix || hasPostfix || hasTranspiler) && !hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} is missing a [Feature] attribute");
                    if (!(hasPrefix || hasPostfix || hasTranspiler) && hasFeature)
                        Log.Warning($"{type.Name}::{method.Name} has [Feature] but no Harmony attribute");

                    if (hasPrefix && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Prefix");
                    if (hasPostfix && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Postfix");
                    if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                        Log.Warning($"{type.Name}::{method.Name} should be named with _Transpiler");
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
