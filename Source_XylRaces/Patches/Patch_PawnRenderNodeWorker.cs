using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    // Not sure if this is a good thing to modify, seems performance-sensitive

    [HarmonyPatch(typeof(PawnRenderNodeWorker))]
    public static class Patch_PawnRenderNodeWorker
    {
        public static bool Enabled => enabled.Value;
        public static Lazy<bool> enabled = new(Config.GeneWithModExtensionExists<GeneDefExtension_Rendering>);

        [Feature(typeof(GeneDefExtension_Rendering))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
        public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (!Enabled)
                return;

            if (parms.pawn == null)
                return;

            foreach (var extension in parms.pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Rendering>())
            {
                foreach (var modifier in extension.modifiers)
                {
                    if (modifier.Matches(node))
                        __result *= modifier.scale;
                }
            }
        }

        [Feature(typeof(GeneDefExtension_Rendering))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnRenderNodeWorker.OffsetFor))]
        public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (!Enabled)
                return;

            if (parms.pawn == null)
                return;

            foreach (var extension in parms.pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_Rendering>())
            {
                foreach (var modifier in extension.modifiers)
                {
                    if (modifier.Matches(node))
                        __result += modifier.offset;
                }
            }
        }
    }
}
