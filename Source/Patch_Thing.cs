using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Thing))]
    static class Patch_Thing
    {
        [HarmonyPrefix, HarmonyPatch("IngestedCalculateAmounts")]
        public static void IngestedCalculateAmounts_Prefix(Thing __instance, Pawn ingester, ref float nutritionWanted)
        {
            var dietDependencies = ingester.genes?.GenesListForReading.OfType<Gene_DietDependency>();
            if (dietDependencies == null)
                return;

            foreach (var dietDependency in dietDependencies)
            {
                if (!dietDependency.ValidateFood(__instance))
                    continue;

                float severityReductionPerNutrition = dietDependency.DefModExtension.severityReductionPerNutrition;
                float nutritionForNeed = dietDependency.LinkedHediff.Severity / severityReductionPerNutrition;
                nutritionWanted = Math.Max(nutritionWanted, nutritionForNeed);
            }
        }
    }
}
