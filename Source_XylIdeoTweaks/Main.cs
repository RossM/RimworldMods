using JetBrains.Annotations;
using HarmonyLib;
using Verse;

namespace Source_ExposableChecker
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main(ModContentPack content) : Mod(content)
    {
        static Main()
        {
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");
            harmony.PatchAll();
        }
    }
}
