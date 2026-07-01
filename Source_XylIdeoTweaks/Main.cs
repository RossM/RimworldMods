using HarmonyLib;
using JetBrains.Annotations;
using TranspilerUtil;
using Verse;

namespace Source_ExposableChecker;

[UsedImplicitly]
[StaticConstructorOnStartup]
public class Main(ModContentPack content) : Mod(content)
{
    static Main()
    {
        var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");
        harmony.PatchAll();

        InfixPatcher.PatchInfix(harmony, typeof(Main).Assembly);
    }
}
