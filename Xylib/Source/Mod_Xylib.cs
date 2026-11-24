namespace Xylib;

[UsedFromReflection]
public class Mod_Xylib : Mod
{
    public Mod_Xylib(ModContentPack content) : base(content)
    {
        var conversionChain
            = (List<BackCompatibilityConverter>)typeof(BackCompatibility).GetField("conversionChain", AccessTools.all)!.GetValue(null)!;
        conversionChain.Add(new BackCompatibilityConverter_GeneWithComps());

        using (new ProfileBlock("Xylib Disharmony patching"))
        {
            Patcher.PatchAll(typeof(Mod_Xylib).Assembly);
        }
    }
}
