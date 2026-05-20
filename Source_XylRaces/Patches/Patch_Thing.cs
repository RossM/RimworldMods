using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Thing))]
    public static class Patch_Thing
    {
        private static readonly InstructionMatcher.Rule Rule_GetStatValue
            = InstructionMatcher.MakeRedirectRule(StatExtension.GetStatValue, GetStatValue_Wrapper);

        [Feature(typeof(DietDependency))]
        [HarmonyPrefix]
        [HarmonyPatch("IngestedCalculateAmounts")]
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

        [Feature(nameof(GeneDefExtension_HostilityOverride), nameof(SeeingRed))]
        [HarmonyPrefix]
        [HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            NotificationManager.Instance.Notify(NotificationEvent.DamageTaken, __instance, dinfo);
        }

        [Feature(nameof(FoodHelpers.GetFoodPoisonChanceOffset))]
        [HarmonyTranspiler]
        [HarmonyPatch("Ingested")]
        public static IEnumerable<CodeInstruction> Ingested_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_GetStatValue
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
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

        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Thing.PostMake))]
        public static void PostMake_Postfix(Thing __instance)
        {
            if (__instance is INotificationTarget target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
