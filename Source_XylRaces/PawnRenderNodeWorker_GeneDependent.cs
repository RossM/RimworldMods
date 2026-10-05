namespace XylXenos;

public class PawnRenderNodeWorker_GeneDependent : PawnRenderNodeWorker
{
    public override bool ShouldListOnGraph(PawnRenderNode node, PawnDrawParms parms)
    {
        return EnabledByGenes(node, parms) && base.ShouldListOnGraph(node, parms);
    }

    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        return EnabledByGenes(node, parms) && base.CanDrawNow(node, parms);
    }

    private static bool EnabledByGenes(PawnRenderNode node, PawnDrawParms parms)
    {
        if (parms.pawn.genes is not { } genes || node.Props is not PawnRenderNodeProperties_GeneDependent props)
            return true;

        if (props.requiredGenes is { Count: > 0 } && props.requiredGenes.Any(gene => !genes.HasActiveGene(gene)))
            return false;
        if (props.disallowedGenes is { Count: > 0 } && props.disallowedGenes.Any(gene => genes.HasActiveGene(gene)))
            return false;

        return true;
    }

    #region PawnRenderNodeWorker_AttachmentBody

    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node.Props is PawnRenderNodeProperties_GeneDependent { useBodyGraphicScale: true })
        {
            Vector3 vector = base.ScaleFor(node, parms);
            Vector2 bodyScale = parms.pawn.story.bodyType.bodyGraphicScale;
            return vector * ((bodyScale.x + bodyScale.y) / 2f);
        }

        return base.ScaleFor(node, parms);
    }

    #endregion

    #region PawnRenderNodeWorker_FlipWhenCrawling

    protected override Material GetMaterial(PawnRenderNode node, PawnDrawParms parms)
    {
        if (parms.flipHead && node.Props is PawnRenderNodeProperties_GeneDependent { flipWhenCrawling: true })
            parms.facing = parms.facing.Opposite;
        return base.GetMaterial(node, parms);
    }

    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (parms.flipHead && node.Props is PawnRenderNodeProperties_GeneDependent { flipWhenCrawling: true })
            parms.facing = parms.facing.Opposite;
        return base.LayerFor(node, parms);
    }

    #endregion
}
