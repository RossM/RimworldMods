namespace XylXenos.Patches;

[Patch(typeof(PawnRenderTree))]
public static class Patch_PawnRenderTree
{
    // This allows tattoos to draw properly on top of scaleskin
    [Feature("TODO")]
    [Postfix]
    [Target(nameof(PawnRenderTree.ParallelPreDraw))]
    public static void ParallelPreDraw_Postfix(PawnDrawParms parms, List<PawnGraphicDrawRequest> ___drawRequests)
    {
        if ((parms.flags & PawnRenderFlags.Portrait) == 0)
            return;
        ___drawRequests.SortBy(req => req.preDrawnComputedMatrix.m13);
    }
}
