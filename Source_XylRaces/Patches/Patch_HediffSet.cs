using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(HediffSet))]
    public class Patch_HediffSet
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(HediffSet.DirtyCache))]
        public static void DirtyCache_Postfix(HediffSet __instance)
        {
            __instance.pawn.GetComp<CompPawn_LookupCache>()?.Notify_HediffsChanged();
        }
    }
}
