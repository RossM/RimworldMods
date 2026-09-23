namespace Xylib;

[UsedFromXml]
public class PawnRenderSubWorker_Head : PawnRenderSubWorker
{
    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.tree.pawn.GeneTracker_Xylib is { } geneTracker)
        {
            offset *= geneTracker.bodyScale;
            offset += geneTracker.headOffset;
        }
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        if (node.tree.pawn.GeneTracker_Xylib is { } geneTracker)
            scale *= geneTracker.headScale;
    }
}
