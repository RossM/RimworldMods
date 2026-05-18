using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker))]
    public static class Patch_Pawn_GeneTracker
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch("Notify_GenesChanged")]
        public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
        {
            NotificationManager.Instance.Notify(NotificationCategory.GenesChanged, __instance.pawn);
        }

        [Feature(nameof(DefOf.XylGlobalAddictionChanceFactor), nameof(ChemicalDefExtension))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
        public static void AddictionChanceFactor_Postfix(Pawn_GeneTracker __instance, ChemicalDef chemical, ref float __result)
        {
            if (!__instance.pawn.ChemicalIsAllowedByGenes(chemical))
                __result = 0;
            else
                __result *= __instance.pawn.GetStatValue(DefOf.XylGlobalAddictionChanceFactor);
        }
    }
}
