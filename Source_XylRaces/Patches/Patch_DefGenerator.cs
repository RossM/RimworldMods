namespace XylXenos.Patches;

[HarmonyPatch(typeof(DefGenerator))]
public static class Patch_DefGenerator
{
    [Feature(typeof(GeneDefGenerator))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static void GenerateImpliedDefs_PreResolve_Postfix(bool hotReload)
    {
        PatchHelpers.RunDefGenerators(hotReload);
    }
}
