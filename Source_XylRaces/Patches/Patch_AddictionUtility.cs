using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(AddictionUtility))]
    public static class Patch_AddictionUtility
    {
        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(
            Pawn pawn,
            ChemicalDef chemicalDef,
            ref float effect,
            bool applyGeneToleranceFactor,
            bool divideByBodySize)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
                effect = 0;
            else
                effect *= pawn.GetStatValue(DefOf.XylDrugEffectMultiplier);
        }

        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(AddictionUtility.CanBingeOnNow))]
        public static bool CanBingeOnNow_Prefix(
            Pawn pawn,
            ChemicalDef chemical,
            DrugCategory drugCategory,
            ref bool __result)
        {
            __result = false;

            if (!pawn.ChemicalIsAllowedByGenes(chemical))
                return false;

            return true;
        }
    }
}
