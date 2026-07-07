using System.Reflection;

namespace Xylib.Patches;

[HarmonyPatch(typeof(Building_GeneExtractor))]
internal static class Patch_Building_GeneExtractor
{
    [Feature(nameof(DefModExtension_GeneWithComps.geneType))]
    [HarmonyPostfix]
    public static void SelectionWeight_Postfix(Gene g, ref float __result)
    {
        if (g.def.Extension_GeneWithComps?.geneType is GeneType.Endogene)
            __result = 0f;
    }

    [HarmonyTargetMethod]
    public static MethodInfo TargetMethod()
    {
        var type = AccessTools.TypeByName("RimWorld.Building_GeneExtractor");
        foreach (var nestedType in type.GetNestedTypes(AccessTools.all))
        {
            if (!nestedType.Name.StartsWith("<>c"))
                continue;

            foreach (var method in nestedType.GetMethods(AccessTools.all))
            {
                if (method.Name.StartsWith("<Finish>g__SelectionWeight"))
                    return method;
            }
        }

        return null;
    }
}
