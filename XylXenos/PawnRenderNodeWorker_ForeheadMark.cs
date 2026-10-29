namespace XylXenos;

[UsedFromXml]
public class PawnRenderNodeWorker_ForeheadMark : PawnRenderNodeWorker_FlipWhenCrawling
{
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        var result = base.OffsetFor(node, parms, out pivot);

        if (node.Props.narrowCrownHorizontalOffset != 0f && parms.pawn.story!.headType.narrow && parms.facing.IsHorizontal)
        {
            if (parms.facing == Rot4.East)
                result.x -= node.Props.narrowCrownHorizontalOffset;
            else if (parms.facing == Rot4.West)
                result.x += node.Props.narrowCrownHorizontalOffset;
        }

        return result;
    }
}
