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

            CheckForFeatureAttribute();
        }

        private static void CheckForFeatureAttribute()
        {
            var assembly = MethodBase.GetCurrentMethod().ReflectedType.Assembly;
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                foreach (MethodInfo method in type.DeclaredMethods)
                {
                    var hasFeature = method.HasAttribute<FeatureAttribute>();
                    var hasPrefix = method.HasAttribute<HarmonyPrefix>();
                    var hasPostfix = method.HasAttribute<HarmonyPostfix>();
                    var hasTranspiler = method.HasAttribute<HarmonyTranspiler>();

                    if ((hasPrefix || hasPostfix || hasTranspiler) && !hasFeature)
                    {
                        Log.Warning($"{type.Name}.{method.Name} is missing a [Feature] attribute");
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
