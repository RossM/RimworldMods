using System.Reflection;

namespace Xylib;

[UsedImplicitly]
public class XylibMod : Mod
{
    public XylibMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

        using (new ProfileBlock("Harmony patching"))
            harmony.PatchCategory(null);

        using (new ProfileBlock("Infix patching"))
            InfixPatcher.PatchInfix(harmony, Assembly.GetExecutingAssembly());
    }
}
