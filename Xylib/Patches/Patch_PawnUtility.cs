namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnUtility))]
public static class Patch_PawnUtility
{
    [Feature(typeof(DefModExtension_Chemical))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
    public static void CanTakeDrug_Postfix(Pawn pawn, ThingDef drug, ref bool __result)
    {
        if (!pawn.ChemicalIsAllowedByGenes(drug))
            __result = false;
    }
}