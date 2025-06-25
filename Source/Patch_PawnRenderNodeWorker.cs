using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    // Not sure if this is a good thing to modify, seems performance-sensitive

    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
    public class Patch_PawnRenderNodeWorker
    {
        [HarmonyPostfix]
        static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref UnityEngine.Vector3 __result)
        {
            List<Gene> genes = parms.pawn?.genes?.GenesListForReading;
            if (genes == null)
                return;

            foreach (var gene in genes)
            {
                var extension = gene.def.GetModExtension<ModExtension_GeneDef_Rendering>();
                if (extension != null)
                {
                    __result *= extension.scale;
                }
            }
        }
    }
}
