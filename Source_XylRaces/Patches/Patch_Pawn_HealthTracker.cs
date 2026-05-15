using HarmonyLib;
using JetBrains.Annotations;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_HealthTracker))]
    public static class Patch_Pawn_HealthTracker
    {
        [Feature(nameof(AddHediff))]
        [HarmonyPostfix]
        [UsedImplicitly]
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
