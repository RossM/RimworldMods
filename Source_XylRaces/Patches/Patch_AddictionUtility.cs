using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(AddictionUtility))]
    public static class Patch_AddictionUtility
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly] public static StatDef XylDrugEffectMultiplier;
        }

        [Feature(nameof(Defs.XylDrugEffectMultiplier)), HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(Pawn pawn, ChemicalDef chemicalDef, ref float effect,
            bool applyGeneToleranceFactor, bool divideByBodySize)
        {
            using (new ProfileBlock())
            {
                if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
                    effect = 0;
                else
                    effect *= pawn.GetStatValue(Defs.XylDrugEffectMultiplier);
            }
        }
    }
}
