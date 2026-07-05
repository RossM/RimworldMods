namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnRenderNodeWorker))]
internal static class Patch_PawnRenderNodeWorker
{
    [Feature(nameof(DefModExtension_GeneWithComps.renderNodeModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnRenderNodeWorker.OffsetFor))]
    public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
    {
        List<RenderNodeModifier> renderNodeModifiers = parms.pawn?.GeneTracker_GeneWithComps?.renderNodeModifiers;
        if (renderNodeModifiers == null)
            return;

        foreach (var modifier in renderNodeModifiers)
        {
            if (modifier.Matches(node))
                __result += modifier.offset;
        }
    }

    [Feature(nameof(DefModExtension_GeneWithComps.renderNodeModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
    public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
    {
        List<RenderNodeModifier> renderNodeModifiers = parms.pawn?.GeneTracker_GeneWithComps?.renderNodeModifiers;
        if (renderNodeModifiers == null)
            return;

        foreach (var modifier in renderNodeModifiers)
        {
            if (modifier.Matches(node))
                __result *= modifier.scale;
        }
    }
}
