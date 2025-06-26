using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Body), "CanDrawNow")]
    public class Patch_PawnRenderNodeWorker_Apparel_Body
    {
        [HarmonyPrefix]
        public static bool Prefix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            var comp = parms.pawn.GetComp<Comp_RenderProperties>();
            if (comp is { hideClothes: true })
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
