using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly] 
            public static GeneDef XylAquatic;
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance(JoyGiver __instance, Pawn pawn, ref float __result)
        {
            if (__instance is JoyGiver_GoSwimming && pawn.genes?.HasActiveGene(Defs.XylAquatic) == true)
                __result *= 5;
        }
    }
}
