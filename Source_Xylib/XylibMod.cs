using System.Reflection;

namespace Xylib;

[StaticConstructorOnStartup]
public static class LateInit
{
    static LateInit()
    {
        using (new ProfileBlock("Xylib initialize PawnExtraData"))
        {
            foreach (var type in GenTypes.AllTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(IPawnData).IsAssignableFrom(t)))
            {
                try
                {
                    var extraType = typeof(PawnExtraData<>).MakeGenericType(type);
                    RuntimeHelpers.RunClassConstructor(extraType.TypeHandle);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in static constructor of PawnExtraData<{type}>: {ex}");
                }
            }
        }
    }
}

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
