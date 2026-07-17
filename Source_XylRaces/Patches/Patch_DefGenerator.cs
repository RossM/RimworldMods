namespace XylXenos.Patches;

[HarmonyPatch(typeof(DefGenerator))]
public static class Patch_DefGenerator
{
    [Feature(typeof(GeneDefGenerator))]
    [Postfix]
    [Target(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static void GenerateImpliedDefs_PreResolve_Postfix(bool hotReload)
    {
        PatchHelpers.FixupChemicalGenes();
    }
}
