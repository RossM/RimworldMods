namespace XylIdeos;

[UsedImplicitly]
[StaticConstructorOnStartup]
public class Main(ModContentPack content) : Mod(content)
{
    static Main()
    {
        var harmony = new Harmony("xylthixlm.ideos");
        harmony.PatchAll();

        InfixPatcher.PatchInfix(harmony, typeof(Main).Assembly);

        Analyzer.CheckCodingStyle(typeof(Main).Assembly);
    }
}
