using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker))]
    public static class Patch_PawnRenderNodeWorker
    {
        [Feature(nameof(DefExt.renderNodeModifiers))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
        public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (parms.pawn == null)
                return;

            List<RenderNodeModifier> renderNodeModifiers = parms.pawn.GeneSet()?.renderNodeModifiers;
            if (renderNodeModifiers == null)
                return;

            foreach (var modifier in renderNodeModifiers)
            {
                if (modifier.Matches(node))
                    __result *= modifier.scale;
            }
        }

        [Feature(nameof(DefExt.renderNodeModifiers))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PawnRenderNodeWorker.OffsetFor))]
        public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (parms.pawn == null)
                return;

            List<RenderNodeModifier> renderNodeModifiers = parms.pawn.GeneSet()?.renderNodeModifiers;
            if (renderNodeModifiers == null)
                return;

            foreach (var modifier in renderNodeModifiers)
            {
                if (modifier.Matches(node))
                    __result += modifier.offset;
            }
        }
    }
}
