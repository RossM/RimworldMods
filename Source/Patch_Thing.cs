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
            foreach (var dietDependency in ingester.GenesOfType<Gene_DietDependency>())
            {
                if (!dietDependency.ValidateFood(__instance))
                    continue;

                float severityReductionPerNutrition = dietDependency.DefExt.severityReductionPerNutrition;
                float nutritionForNeed = dietDependency.LinkedHediff.Severity / severityReductionPerNutrition;
                nutritionWanted = Math.Max(nutritionWanted, nutritionForNeed);
            }
        }

        [HarmonyPrefix, HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            List<Gene> instigatorGenes = (dinfo.Instigator as Pawn)?.genes?.GenesListForReading;
            if (instigatorGenes != null)
            {
                foreach (var gene in instigatorGenes.OfType<Gene_HostilityOverride>())
                {
                    gene.Notify_PawnDamagedThing(__instance, dinfo, __result);
                }
            }

            List<Gene> instanceGenes = (__instance as Pawn)?.genes?.GenesListForReading;
            if (instanceGenes != null)
            {
                foreach (var gene in instanceGenes.OfType<Gene_Berserker>())
                {
                    gene.Notify_DamageTaken(dinfo, __result);
                }
            }

        }
    }
}
