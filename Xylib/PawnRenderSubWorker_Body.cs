namespace Xylib;

[UsedFromXml]
public class PawnRenderSubWorker_Body : PawnRenderSubWorker
{
    private static readonly int mainTex = Shader.PropertyToID("_MainTex");

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.tree.pawn.GeneTracker_Xylib is { } geneTracker)
            offset += geneTracker.bodyOffset;
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        if (node.tree.pawn.GeneTracker_Xylib is { } geneTracker)
            scale *= geneTracker.bodyScale;
    }

    public override void EditMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms, ref MaterialPropertyBlock block)
    {
        if (node.tree.pawn.GeneTracker_Xylib is { bodyGraphicOverride: { } graphic })
            block.SetTexture(mainTex, graphic.NodeGetMat(parms).mainTexture);
    }
}
