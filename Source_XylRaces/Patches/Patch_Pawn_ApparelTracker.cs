using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker))]
    public static class Patch_Pawn_ApparelTracker
    {
        [Feature(nameof(Flight)), HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
        public static void Notify_ApparelChanged_Postfix(Pawn_ApparelTracker __instance)
        {
            Pawn pawn = __instance.pawn;

            foreach (var toNotify in pawn.EverythingOfType<INotifyApparelChanged>())
                toNotify.Notify_ApparelChanged(pawn);
        }
    }
}
