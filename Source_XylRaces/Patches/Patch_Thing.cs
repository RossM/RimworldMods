using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Thing))]
    public static class Patch_Thing
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("IngestedCalculateAmounts")]
        public static void IngestedCalculateAmounts_Prefix(Thing __instance, Pawn ingester, ref float nutritionWanted)
        {
            using (new ProfileBlock())
            {
                foreach (var dietDependency in ingester.GenesOfType<DietDependency>())
                {
                    if (!dietDependency.ValidateFood(__instance))
                        continue;

                    float nutritionForNeed = dietDependency.NutritionWantedToSatisfy();
                    nutritionWanted = Math.Max(nutritionWanted, nutritionForNeed);
                }
            }
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            using (new ProfileBlock())
            {
                if (dinfo.Instigator is Pawn instigator)
                {
                    foreach (var listener in instigator.EverythingOfType<INotifyPawnDamagedThing>())
                    {
                        listener.Notify_PawnDamagedThing(__instance, dinfo, __result);
                    }

                    // Pets get protection from insect pheromones too, so break the protection if the pet
                    // damages an insect.
                    if (instigator.playerSettings?.Master != null)
                    {
                        foreach (var listener in instigator.playerSettings.Master
                                     .EverythingOfType<INotifyPawnDamagedThing>())
                        {
                            listener.Notify_PawnDamagedThing(__instance, dinfo, __result);
                        }
                    }
                }

                if (__instance is Pawn target)
                {
                    foreach (var listener in target.EverythingOfType<INotifyDamageTaken>())
                    {
                        listener.Notify_DamageTaken(dinfo, __result);
                    }
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
                        CodeInstruction.Call(typeof(FoodHelpers), nameof(FoodHelpers.GetFoodPoisonChanceOffset)),
                        new CodeInstruction(OpCodes.Add),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("Ingested")]
        public static IEnumerable<CodeInstruction> Ingested_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            FixupIngested.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }
    }
}
