namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnRenderTree))]
internal static class Patch_PawnRenderTree
{
    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [InfixPostfix(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    [InfixPatch(nameof(PawnRenderTree.ParallelPreDraw))]
    public static void AppendRequests_Postfix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        List<RenderNodeModifier> renderNodeModifiers = parms.pawn?.GeneTracker_GeneWithComps?.renderNodeModifiers_hidden;
        if (renderNodeModifiers == null)
            return;

        for (int i = requests.Count - 1; i >= 0; i--)
        {
            bool hidden = false;

            foreach (var renderNodeModifier in renderNodeModifiers)
            {
                if (renderNodeModifier.Matches(requests[i].node))
                {
                    hidden = true;
                    break;
                }
            }

            if (hidden)
                requests.RemoveAt(i);
        }
    }
}
