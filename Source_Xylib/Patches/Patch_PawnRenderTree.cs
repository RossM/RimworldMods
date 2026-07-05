namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnRenderTree))]
internal static class Patch_PawnRenderTree
{
    private static readonly List<RenderNodeModifier> renderNodeModifiersTemp = [];

    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [InfixPostfix(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    [InfixPatch(nameof(PawnRenderTree.ParallelPreDraw))]
    public static void AppendRequests_Postfix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        List<RenderNodeModifier> renderNodeModifiers = parms.pawn?.GeneTracker_GeneWithComps?.renderNodeModifiers;
        if (renderNodeModifiers == null)
            return;

        renderNodeModifiersTemp.Clear();
        renderNodeModifiersTemp.AddRange(renderNodeModifiers.Where(m => m.hidden));
        if (renderNodeModifiersTemp.Count == 0)
            return;

        for (int i = requests.Count - 1; i >= 0; i--)
        {
            bool hidden = false;

            foreach (var renderNodeModifier in renderNodeModifiersTemp)
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

        renderNodeModifiersTemp.Clear();
    }
}
