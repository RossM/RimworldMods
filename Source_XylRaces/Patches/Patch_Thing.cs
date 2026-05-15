using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Thing))]
    public static class Patch_Thing
    {
        [Feature(nameof(DietDependency)), HarmonyPrefix, UsedImplicitly, HarmonyPatch("IngestedCalculateAmounts")]
        public static void IngestedCalculateAmounts_Prefix(Thing __instance, Pawn ingester, ref float nutritionWanted)
        {
            foreach (var dietDependency in ingester.ActiveGenesOfType<DietDependency>())
            {
                if (!dietDependency.ValidateFood(__instance))
                    continue;

                float nutritionForNeed = dietDependency.NutritionWantedToSatisfy();
                nutritionWanted = Math.Max(nutritionWanted, nutritionForNeed);
            }
        }

        [Feature(nameof(GeneDefExtension_HostilityOverride), nameof(SeeingRed)), HarmonyPrefix, UsedImplicitly, HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            if (dinfo.Instigator is Pawn instigator)
            {
                HostilityOverrideManager.GetManager(instigator.Map)?.Notify_PawnDamagedThing(instigator, __instance);
            }

            if (__instance is Pawn target)
            {
                foreach (var listener in target.EverythingOfType<INotifyDamageTaken>())
                {
                    listener.Notify_DamageTaken(dinfo, __result);
                }
            }
        }

        private static readonly InstructionMatcher FixupIngested = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                    AccessTools.Method(typeof(Patch_Thing), nameof(GetStatValue_Wrapper)))
            }
        };

        [Feature(nameof(FoodHelpers.GetFoodPoisonChanceOffset)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("Ingested")]
        public static IEnumerable<CodeInstruction> Ingested_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            FixupIngested.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetStatValue_Wrapper(Pawn ingester, Thing thing, StatDef stat, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.FoodPoisonChanceFixedHuman)
                value += FoodHelpers.GetFoodPoisonChanceOffset(ingester, thing);
            return value;
        }
    }
}
