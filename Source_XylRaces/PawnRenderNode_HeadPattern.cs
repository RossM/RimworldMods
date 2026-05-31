namespace XylXenos;

[UsedFromXml]
public class PawnRenderNode_HeadPattern(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Head(pawn, props, tree)
{
    public override Graphic GraphicFor(Pawn pawn)
    {
        if (!pawn.health.hediffSet.HasHead)
            return null;

        string maskPath = pawn.story.headType.graphicPath;
        return GraphicDatabase.Get<Graphic_Multi>(TexPathFor(pawn), ShaderDatabase.CutoutSkinOverlay, Vector2.one, ColorFor(pawn),
            Color.white, null, maskPath);
    }
}
