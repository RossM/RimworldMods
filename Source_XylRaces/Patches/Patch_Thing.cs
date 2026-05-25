using System;
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
        [Feature(nameof(GeneDefExt.dietDependency))]
        [HarmonyPrefix]
        [HarmonyPatch("IngestedCalculateAmounts")]
        public static void IngestedCalculateAmounts_Prefix(Thing __instance, Pawn ingester, ref float nutritionWanted)
        {
            foreach (var hediff in ingester.HediffsOfType<Hediff_DietDependency>())
            {
                if (!hediff.ValidateFood(__instance))
                    continue;

                nutritionWanted = Math.Max(nutritionWanted, hediff.NutritionWantedToSatisfy());
            }
        }

        [Feature(typeof(HostilityOverrideManager))]
        [Feature(typeof(SeeingRed))]
        [HarmonyPrefix]
        [HarmonyPatch("TakeDamage")]
        public static void TakeDamage_Prefix(Thing __instance, DamageInfo dinfo)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PreDamageTaken, __instance, dinfo);
        }

        [Feature(nameof(FoodHelpers.GetFoodPoisonChanceOffset))]
        [InfixPostfix(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
        [InfixPatch("Ingested")]
        public static void GetStatValue_Postfix(Pawn ingester, Thing thing, StatDef stat, ref float __result)
        {
            if (stat == StatDefOf.FoodPoisonChanceFixedHuman)
                __result += FoodHelpers.GetFoodPoisonChanceOffset(ingester, thing);
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
