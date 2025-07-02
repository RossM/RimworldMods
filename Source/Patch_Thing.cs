using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
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
                foreach (var gene in instanceGenes.OfType<Gene_SeeingRed>())
                {
                    gene.Notify_DamageTaken(dinfo, __result);
                }
            }
        }

        private static readonly InstructionMatcher FixupIngested = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.Call(typeof(FoodUtility), nameof(FoodUtility.GetFoodPoisonChanceFactor)), 
                        new CodeInstruction(OpCodes.Mul),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(typeof(FoodHelpers), nameof(FoodHelpers.GetFoodPoisoningChanceOffset)),
                        new CodeInstruction(OpCodes.Sub),
                    ]
                }
            }
        };

        [HarmonyTranspiler, HarmonyPatch("Ingested")]
        static IEnumerable<CodeInstruction> Ingested_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!FixupIngested.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }
    }
}
