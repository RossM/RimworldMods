using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(SlaveRebellionUtility))]
    public static class Patch_SlaveRebellionUtility
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
            public static GeneDef XylDocile;
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
        public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
        {
            if (Defs.XylDocile != null && pawn.genes.HasActiveGene(Defs.XylDocile))
                __result *= 5;
        }
    }
}
