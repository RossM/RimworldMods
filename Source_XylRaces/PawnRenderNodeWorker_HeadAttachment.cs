using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class PawnRenderNodeWorker_HeadAttachment : PawnRenderNodeWorker_FlipWhenCrawling
{
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, [UnscopedRef] out Vector3 pivot)
    {
        var result = base.OffsetFor(node, parms, out pivot);

        if (parms.pawn.story.headType.narrow && node.Props.narrowCrownHorizontalOffset != 0f && parms.facing.IsHorizontal)
        {
            if (parms.facing == Rot4.East)
                result.x -= node.Props.narrowCrownHorizontalOffset;
            else if (parms.facing == Rot4.West)
            {
                result.x += node.Props.narrowCrownHorizontalOffset;
            }
            result.z -= node.Props.narrowCrownHorizontalOffset;
        }

        return result;
    }
}
