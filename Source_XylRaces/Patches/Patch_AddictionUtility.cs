using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(AddictionUtility))]
    public static class Patch_AddictionUtility
    {
        [Feature(typeof(ChemicalDefExtension))]
        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(
            Pawn pawn,
            ChemicalDef chemicalDef,
            ref float effect)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
                effect = 0f;
            else
                effect *= pawn.GetStatValue(DefOf.XylDrugEffectMultiplier);
        }

        [Feature(typeof(ChemicalDefExtension))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AddictionUtility.CanBingeOnNow))]
        public static void CanBingeOnNow_Postfix(
            Pawn pawn,
            ChemicalDef chemical,
            DrugCategory drugCategory,
            ref bool __result)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemical))
                __result = false;
        }
    }
}
