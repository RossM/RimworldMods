using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse.AI;
using XylRacesCore;

namespace Source_XylRaces.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker))]
    public static class Patch_Pawn_GeneTracker
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch("Notify_GenesChanged")]
        public static void Notify_GenesChanged_Postfix(Pawn_GeneTracker __instance)
        {
            __instance.pawn.GetComp<CompPawn_GeneCache>()?.Notify_GenesChanged();
        }
    }
}
