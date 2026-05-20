using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HediffComp_TendDuration))]
    public static class Patch_HediffComp_TendDuration
    {
        [Feature(typeof(HediffComp_TendDuration_Petrification))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(HediffComp_TendDuration.AllowTend), MethodType.Getter)]
        public static void AllowTend_Postfix(HediffComp_TendDuration __instance, ref bool __result)
        {
            if (__instance is HediffComp_TendDuration_Petrification p)
            {
                __result &= p.AllowTendExt;
            }
        }
    }
}
