namespace Yobai;

public sealed class Main : Mod
{
    public Main(ModContentPack content) : base(content)
    {
        Patcher.PatchAll(typeof(Main).Assembly);
    }
}
