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
            using (new ProfileBlock())
            {
                if (parms.pawn == null)
                    return;
                foreach (var extension in parms.pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Rendering>())
                    __result *= extension.scale;
            }
        }
    }
}
