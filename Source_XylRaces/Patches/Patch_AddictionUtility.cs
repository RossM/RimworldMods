using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
        public static void ModifyChemicalEffectForToleranceAndBodySize_Postfix(Pawn pawn, ref float effect,
            bool applyGeneToleranceFactor, bool divideByBodySize)
        {
            using (new ProfileBlock())
            {
                effect *= pawn.GetStatValue(Defs.XylDrugEffectMultiplier);
            }
        }
    }
}
