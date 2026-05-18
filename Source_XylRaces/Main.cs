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
