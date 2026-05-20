using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(AddictionUtility))]
    public static class Patch_AddictionUtility
    {
        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static bool ModifyChemicalEffectForToleranceAndBodySize_Prefix(
            Pawn pawn,
            ChemicalDef chemicalDef,
            ref float effect)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
            {
                effect = 0f;
                return false;
            }

            return true;
        }

        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(Pawn pawn, ref float effect)
        {
            effect *= pawn.GetStatValue(DefOf.XylDrugEffectMultiplier);
        }

        [Feature(nameof(DefOf.XylDrugEffectMultiplier))]
        [HarmonyPrefix]
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
