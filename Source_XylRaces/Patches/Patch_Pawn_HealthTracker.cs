using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn_HealthTracker))]
    public static class Patch_Pawn_HealthTracker
    {
        [Feature(typeof(AddHediff))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn_HealthTracker.CheckForStateChange))]
        public static void CheckForStateChange_Postfix(Pawn_HealthTracker __instance)
        {
            foreach (var gene in __instance.pawn.ActiveGenesOfType<AddHediff>())
            {
                gene.NotifyStateChange();
            }
        }
    }
}
