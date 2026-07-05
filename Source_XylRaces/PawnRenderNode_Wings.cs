namespace XylXenos;

[UsedFromXml]
public class PawnRenderNode_Wings(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode(pawn, props, tree)
{
    protected override string TexPathFor(Pawn pawn)
    {
        // Logically, when the apparel covers the wings and prevents flight, the wings shouldn't
        // be visible. However, that makes the character look like they're not chyrr. Disabling
        // this for now.

        //if (pawn.FirstActiveGeneOfType<Flight>()?.flightAllowedByApparel is false)
        //    return null;

        if (!props.texPaths.NullOrEmpty())
        {
            return pawn.flight?.Flying is true ? props.texPaths[1] : props.texPaths[0];
        }

        return base.TexPathFor(pawn);
    }
}
