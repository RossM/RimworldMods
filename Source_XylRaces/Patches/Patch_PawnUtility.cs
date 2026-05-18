using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnUtility))]
    public static class Patch_PawnUtility
    {
        [Feature(nameof(ChemicalDefExtension))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
        public static bool CanTakeDrug_Prefix(Pawn pawn, ThingDef drug, ref bool __result)
        {
            if (pawn.ChemicalIsAllowedByGenes(drug))
                return true;

            __result = false;
            return false;
        }
    }
}
