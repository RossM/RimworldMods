using System.Reflection;

namespace Xylib;

[UsedFromReflection]
public class XylibMod : Mod
{
    public XylibMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("Xylthixlm.Xylib");

        using (new ProfileBlock("Xylib Harmony patching"))
            harmony.PatchCategory(null);

        using (new ProfileBlock("Xylib Infix patching"))
            InfixPatcher.PatchInfix(harmony, Assembly.GetExecutingAssembly());

        using (new ProfileBlock("Xylib Check patches"))
            Analyzer.CheckCodingStyle_Patches(typeof(XylibMod).Assembly);
    }
}
