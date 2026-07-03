using System.Reflection;

namespace Xylib;

[StaticConstructorOnStartup]
public static class LateInit
{
    static LateInit()
    {
        using (new ProfileBlock("Xylib initialize PawnExtraData"))
        {
            // Ensure that all PawnExtraData<T> static constructors are run, so that they register their event listeners in time
            // to handle InPawnExposeData during loading. Without this the static constructor may not run until the first time Get<T>
            // is called, which is after the game has already been loaded.
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
