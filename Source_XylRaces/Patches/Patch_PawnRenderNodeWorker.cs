using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    // Not sure if this is a good thing to modify, seems performance-sensitive

    [HarmonyPatch(typeof(PawnRenderNodeWorker))]
    public static class Patch_PawnRenderNodeWorker
    {
        public static Lazy<bool> Enabled = new(Config.GeneWithModExtensionExists<GeneDefExtension_Rendering>);

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
        public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref UnityEngine.Vector3 __result)
        {
            if (Enabled.Value == false)
                return;

            using (new ProfileBlock())
            {
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
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(PawnRenderNodeWorker.OffsetFor))]
        public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref UnityEngine.Vector3 __result)
        {
            if (Enabled.Value == false)
                return;

            using (new ProfileBlock())
            {
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
}
