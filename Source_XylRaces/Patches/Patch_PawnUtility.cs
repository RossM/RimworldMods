using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(PawnUtility))]
    public static class Patch_PawnUtility
    {
        [Feature(nameof(ChemicalModExtension)), HarmonyPrefix, UsedImplicitly,
         HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
        public static bool CanTakeDrug_Prefix(Pawn pawn, ThingDef drug, ref bool __result)
        {
            if (pawn.ChemicalIsAllowedByGenes(drug)) 
                return true;

            __result = false;
            return false;

        }
    }
}
