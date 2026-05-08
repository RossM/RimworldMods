using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker))]
    public static class Patch_Pawn_GeneTracker
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly] public static StatDef XylGlobalAddictionChanceFactor;
        }

        [Feature(nameof(CompPawn_LookupCache)), HarmonyPostfix, UsedImplicitly, HarmonyPatch("Notify_GenesChanged")]
        public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
        {
            __instance.pawn.GetComp<CompPawn_LookupCache>()?.Notify_GenesChanged();
        }

        [Feature(nameof(Defs.XylGlobalAddictionChanceFactor), nameof(ChemicalDefExtension)), HarmonyPostfix,
         UsedImplicitly, HarmonyPatch(nameof(Pawn_GeneTracker.AddictionChanceFactor))]
        public static void AddictionChanceFactor_Postfix(Pawn_GeneTracker __instance, ChemicalDef chemical, ref float __result)
        {
            using (new ProfileBlock())
            {
                if (!__instance.pawn.ChemicalIsAllowedByGenes(chemical))
                    __result = 0;
                else
                    __result *= __instance.pawn.GetStatValue(Defs.XylGlobalAddictionChanceFactor);
            }
        }
    }
}
