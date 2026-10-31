using System.Reflection;

namespace Xylib;

[UsedFromReflection]
public class Mod_Xylib : Mod
{
    public Mod_Xylib(ModContentPack content) : base(content)
    {
        using (new ProfileBlock("Xylib Disharmony patching"))
        {
            Patcher.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
