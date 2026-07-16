namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnUtility))]
internal static class Patch_PawnUtility
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Postfix]
    [Target(nameof(PawnUtility.CanTakeDrug))]
    public static void CanTakeDrug_Postfix(Pawn pawn, ThingDef drug, ref bool __result)
    {
        if (!pawn.ChemicalIsAllowedByGenes(drug))
            __result = false;
    }
}
