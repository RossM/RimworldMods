using System;
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

        [Feature(typeof(GeneDefExtension_HostilityOverride))]
        [Feature(typeof(SeeingRed))]
        [HarmonyPrefix]
        [HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PreDamageTaken, __instance, dinfo);
        }

        [Feature(nameof(FoodHelpers.GetFoodPoisonChanceOffset))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
        [InfixPatch("Ingested")]
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
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (__instance is INotificationListener target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
