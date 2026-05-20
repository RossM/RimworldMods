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
        [HarmonyPatch("Notify_GenesChanged")]
        public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.GenesChanged, __instance.pawn);
        }

        [Feature(nameof(DefOf.XylGlobalAddictionChanceFactor), nameof(ChemicalDefExtension))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
        public static bool AddictionChanceFactor_Prefix(Pawn_GeneTracker __instance, ChemicalDef chemical, out float __result)
        {
            __result = 0f;
            if (!__instance.pawn.ChemicalIsAllowedByGenes(chemical))
                return false;

            return true;
        }

        [Feature(nameof(DefOf.XylGlobalAddictionChanceFactor), nameof(ChemicalDefExtension))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
        public static void AddictionChanceFactor_Postfix(Pawn_GeneTracker __instance, ref float __result)
        {
            __result *= __instance.pawn.GetStatValue(DefOf.XylGlobalAddictionChanceFactor);
        }
    }
}
