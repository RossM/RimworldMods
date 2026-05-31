namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnRenderNodeWorker))]
public static class Patch_PawnRenderNodeWorker
{
    [Feature(nameof(DefModExtension_Gene.renderNodeModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnRenderNodeWorker.ScaleFor))]
    public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
    {
        if (parms.pawn == null)
            return;

        List<RenderNodeModifier> renderNodeModifiers = parms.pawn.GeneTracker?.renderNodeModifiers;
        if (renderNodeModifiers == null)
            return;

        foreach (var modifier in renderNodeModifiers)
        {
            if (modifier.Matches(node))
                __result *= modifier.scale;
        }
    }

    [Feature(nameof(DefModExtension_Gene.renderNodeModifiers))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnRenderNodeWorker.OffsetFor))]
    public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
    {
        if (parms.pawn == null)
            return;

        List<RenderNodeModifier> renderNodeModifiers = parms.pawn.GeneTracker?.renderNodeModifiers;
        if (renderNodeModifiers == null)
            return;

        foreach (var modifier in renderNodeModifiers)
        {
            if (modifier.Matches(node))
                __result += modifier.offset;
        }
    }
}
