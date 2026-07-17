using System.Reflection;

namespace Xylib.Patches;

[HarmonyPatch(typeof(Building_GeneExtractor))]
internal static class Patch_Building_GeneExtractor
{
    [Feature(nameof(DefModExtension_GeneWithComps.geneType))]
    [Postfix]
    [Target("Finish.SelectionWeight")]
    public static void SelectionWeight_Postfix(Gene g, ref float __result)
    {
        if (g.def.Extension_GeneWithComps?.geneType is GeneType.Endogene)
            __result = 0f;
    }
}
