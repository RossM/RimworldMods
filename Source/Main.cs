using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
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
