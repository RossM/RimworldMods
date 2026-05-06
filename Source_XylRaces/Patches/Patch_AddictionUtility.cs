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
                effect *= pawn.GetStatValue(Defs.XylDrugEffectMultiplier);

                var defExtension = chemicalDef.GetModExtension<ChemicalModExtension>();
                if (defExtension == null)
                    return;

                if (!defExtension.prohibitedGenes.NullOrEmpty() && defExtension.prohibitedGenes.Any(pawn.HasActiveGene)) 
                    effect = 0;
                if (!defExtension.requiredGenes.NullOrEmpty() && !defExtension.requiredGenes.Any(pawn.HasActiveGene))
                    effect = 0;
            }
        }
    }
}
