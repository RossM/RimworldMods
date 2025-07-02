using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    // Not sure if this is a good thing to modify, seems performance-sensitive

    [HarmonyPatch(typeof(PawnRenderNodeWorker))]
    public static class Patch_PawnRenderNodeWorker
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
        public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref UnityEngine.Vector3 __result)
        {
            List<Gene> genes = parms.pawn?.genes?.GenesListForReading;
            if (genes == null)
                return;

            foreach (var gene in genes)
            {
                var extension = gene.def.GetModExtension<GeneDefExtension_Rendering>();
                if (extension != null)
                {
                    __result *= extension.scale;
                }
            }
        }
    }
}
