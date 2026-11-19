using System.Xml;

namespace Xylib.Patches;

[Patch(typeof(LoadedModManager))]
public static class Patch_LoadedModManager
{
    [Feature("ParentId")]
    [Postfix]
    [Target(nameof(LoadedModManager.ApplyPatches))]
    public static void ApplyPatches_Postfix(XmlDocument xmlDoc)
    {
        PatchHelpers.AddNamesForInheritance(xmlDoc);
    }
}
