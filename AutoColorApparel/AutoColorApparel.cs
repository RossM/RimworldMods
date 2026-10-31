using Disharmony;
using JetBrains.Annotations;
using Verse;

namespace AutoColorApparel;

[UsedImplicitly]
public class AutoColorApparel : Mod
{
    public AutoColorApparel(ModContentPack content) : base(content)
    {
        Patcher.PatchAll(GetType().Assembly);
    }
}
